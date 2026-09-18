using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Extensions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interview Service — EF Core implementation (PostgreSQL).
    /// DDD model: trạng thái và AI scoring nằm trong entity.
    /// GetAllAsync: lọc dữ liệu tự động theo Role.
    /// </summary>
    public class InterviewService : IInterviewService
    {
        private readonly AppDbContext _context;
        private readonly EventLogger _eventLogger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public InterviewService(AppDbContext context, EventLogger eventLogger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _eventLogger = eventLogger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Nested projection: Interview → InterviewResponseDto (kèm List<InterviewQuestionResponseDto>).
        /// </summary>
        private static InterviewResponseDto MapToResponseDto(Interview interview) => new()
        {
            Id = interview.Id,
            ApplicationId = interview.ApplicationId,
            InterviewerId = interview.InterviewerId,
            InterviewerName = interview.Interviewer?.Employee?.FullName ?? interview.Interviewer?.Email,
            ScheduledDate = interview.ScheduledDate,
            Status = interview.Status.ToString(),
            AiOverallScore = interview.AiOverallScore,
            AiOverallSummary = interview.AiOverallSummary,

            // Nested projection: List<InterviewQuestion> → List<InterviewQuestionResponseDto>
            Questions = interview.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new InterviewQuestionResponseDto
                {
                    Id = q.Id,
                    Question = q.Question,
                    CandidateAnswer = q.CandidateAnswer,
                    AiQuestionScore = q.AiQuestionScore,
                    AiFeedback = q.AiFeedback,
                    OrderIndex = q.OrderIndex
                })
                .ToList()
        };

        private static readonly Dictionary<string, Func<IQueryable<Interview>, bool, IOrderedQueryable<Interview>>> SortMappings =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["scheduleddate"] = (q, desc) => desc ? q.OrderByDescending(i => i.ScheduledDate) : q.OrderBy(i => i.ScheduledDate),
                ["status"] = (q, desc) => desc ? q.OrderByDescending(i => i.Status) : q.OrderBy(i => i.Status),
                ["id"] = (q, desc) => desc ? q.OrderByDescending(i => i.Id) : q.OrderBy(i => i.Id),
            };

        public async Task<PagedResult<InterviewResponseDto>> GetAllAsync(PaginationParams paginationParams)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var role = user?.GetRole();
            var currentUserId = user?.GetUserId();

            IQueryable<Interview> query = _context.Interviews
                .AsNoTracking()
                .Include(i => i.Questions)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee);

            // 1) Filter theo Role trước
            if (role == nameof(UserRole.Interviewer) && currentUserId.HasValue)
            {
                query = query.Where(i => i.InterviewerId == currentUserId.Value);
            }
            else if (role == nameof(UserRole.Candidate) && currentUserId.HasValue)
            {
                query = query.Where(i =>
                    i.Application.Candidate.UserId.HasValue &&
                    i.Application.Candidate.UserId.Value == currentUserId.Value);
            }
            // Admin, HR: no filter — return all

            // 2) ILike Search (nếu có) trên tập đã qua bước 1
            if (!string.IsNullOrWhiteSpace(paginationParams.Search))
            {
                var search = paginationParams.Search.Trim();
                if (_context.Database.IsNpgsql())
                {
                    query = query.Where(i =>
                        EF.Functions.ILike(i.Application.JobPosting.Title, $"%{search}%") ||
                        EF.Functions.ILike(i.Application.Candidate.FullName, $"%{search}%") ||
                        EF.Functions.ILike(i.Application.Candidate.Email, $"%{search}%"));
                }
                else
                {
                    query = query.Where(i =>
                        i.Application.JobPosting.Title.ToLower().Contains(search.ToLower()) ||
                        i.Application.Candidate.FullName.ToLower().Contains(search.ToLower()) ||
                        i.Application.Candidate.Email.ToLower().Contains(search.ToLower()));
                }
            }

            // 3) CountAsync() trên tập đã filter + search
            var totalCount = await query.CountAsync();

            // 4) Whitelist Sorting ("scheduleddate", "status", "id", default "scheduleddate" desc)
            IOrderedQueryable<Interview> orderedQuery;
            if (!string.IsNullOrWhiteSpace(paginationParams.SortBy) &&
                SortMappings.TryGetValue(paginationParams.SortBy, out var sortFunc))
            {
                orderedQuery = sortFunc(query, paginationParams.IsDescending);
            }
            else
            {
                orderedQuery = paginationParams.IsDescending
                    ? query.OrderByDescending(i => i.ScheduledDate)
                    : query.OrderByDescending(i => i.ScheduledDate);
            }

            // 5) Skip/Take -> Select -> ToListAsync()
            var interviews = await orderedQuery
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PagedResult<InterviewResponseDto>
            {
                Items = interviews.Select(MapToResponseDto).ToList(),
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }

        public async Task<InterviewResponseDto?> GetByIdAsync(int id)
        {
            var interview = await _context.Interviews
                .AsNoTracking()
                .Include(i => i.Questions)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee)
                .FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new NotFoundException("Interview", id);

            return MapToResponseDto(interview);
        }

        public async Task<IEnumerable<InterviewResponseDto>> GetByApplicationAsync(int applicationId)
        {
            var interviews = await _context.Interviews
                .AsNoTracking()
                .Include(i => i.Questions)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee)
                .Where(i => i.ApplicationId == applicationId)
                .ToListAsync();

            return interviews.Select(MapToResponseDto);
        }

        public async Task<InterviewResponseDto> CreateAsync(InterviewCreateDto dto)
        {
            // Kiểm tra ApplicationId tồn tại (ràng buộc FK)
            var applicationExists = await _context.Applications.AnyAsync(a => a.Id == dto.ApplicationId);
            if (!applicationExists)
                throw new BusinessRuleException($"Application with ID {dto.ApplicationId} does not exist.");

            // Kiểm tra InterviewerId tồn tại nếu được cung cấp
            if (dto.InterviewerId.HasValue)
            {
                var interviewerExists = await _context.Users.AnyAsync(u =>
                    u.Id == dto.InterviewerId.Value &&
                    u.Role == UserRole.Interviewer);
                if (!interviewerExists)
                    throw new BusinessRuleException($"Interviewer with ID {dto.InterviewerId.Value} does not exist or is not an Interviewer.");
            }

            var interview = new Interview
            {
                ApplicationId = dto.ApplicationId,
                ScheduledDate = dto.ScheduledDate,
                InterviewerId = dto.InterviewerId
            };
            interview.Schedule(); // DDD domain method — set Status = Scheduled

            await _context.Interviews.AddAsync(interview);
            await _context.SaveChangesAsync();

            if (interview.InterviewerId.HasValue)
            {
                await _context.Entry(interview)
                    .Reference(i => i.Interviewer)
                    .Query()
                    .Include(u => u.Employee)
                    .LoadAsync();
            }

            _eventLogger.LogEvent("Interview",
                $"created (ApplicationId: {dto.ApplicationId}, Date: {dto.ScheduledDate:yyyy-MM-dd})",
                _eventLogger.OnEntityCreated);

            return MapToResponseDto(interview);
        }

        /// <summary>
        /// Thêm câu hỏi vào buổi phỏng vấn.
        /// Sau khi thêm, gọi DDD domain method RecalculateOverallScore() để tính lại điểm.
        /// </summary>
        public async Task<bool> AddQuestionAsync(int interviewId, InterviewQuestionCreateDto dto)
        {
            var interview = await _context.Interviews
                .Include(i => i.Questions)
                .FirstOrDefaultAsync(i => i.Id == interviewId);

            if (interview == null) throw new NotFoundException("Interview", interviewId);

            var question = new InterviewQuestion
            {
                InterviewId = interviewId,
                Question = dto.Question,
                OrderIndex = dto.OrderIndex
            };

            interview.Questions.Add(question);

            // DDD domain method — tính lại AiOverallScore = trung bình điểm các câu
            interview.RecalculateOverallScore();

            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("InterviewQuestion",
                $"added to Interview {interviewId} (Question: {dto.Question[..Math.Min(50, dto.Question.Length)]}...)",
                _eventLogger.OnEntityCreated);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var interview = await _context.Interviews.FindAsync(id)
                ?? throw new NotFoundException("Interview", id);

            _context.Interviews.Remove(interview);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Interview", $"deleted (Id: {id})", _eventLogger.OnEntityDeleted);
            return true;
        }

        public async Task<InterviewResponseDto?> CompleteInterviewAsync(int id, string? summary = null)
        {
            var interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee)
                .Include(i => i.Questions)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview == null) return null;

            interview.Complete(summary);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Interview", $"Interview #{id} marked as Completed", _eventLogger.OnEntityUpdated);

            return MapToResponseDto(interview);
        }

        /// <summary>
        /// Fetch Interview entity cho authorization handler (không DTO).
        /// </summary>
        public async Task<Interview?> GetEntityByIdAsync(int id)
        {
            return await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Application)
                    .ThenInclude(a => a.JobPosting)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        /// <summary>
        /// Fetch entity kèm Questions và Candidate phục vụ đánh giá/trả lời câu hỏi.
        /// </summary>
        public async Task<Interview?> GetEntityWithQuestionsAsync(int id)
        {
            return await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Questions)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        /// <summary>
        /// Cập nhật câu trả lời của ứng viên cho câu hỏi phỏng vấn.
        /// </summary>
        public async Task<bool> UpdateQuestionAnswerAsync(InterviewQuestion question, string candidateAnswer)
        {
            question.CandidateAnswer = candidateAnswer;
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("InterviewQuestion",
                $"Candidate answer updated for Question #{question.Id} in Interview #{question.InterviewId}",
                _eventLogger.OnEntityUpdated);

            return true;
        }
    }
}
