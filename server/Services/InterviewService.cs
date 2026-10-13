using System.Net;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Extensions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
        private readonly AudioInterviewOptions _audioInterviewOptions;
        private readonly IEmailService? _emailService;

        public InterviewService(
            AppDbContext context,
            EventLogger eventLogger,
            IHttpContextAccessor httpContextAccessor,
            IOptions<AudioInterviewOptions>? audioInterviewOptions = null,
            IEmailService? emailService = null)
        {
            _context = context;
            _eventLogger = eventLogger;
            _httpContextAccessor = httpContextAccessor;
            _audioInterviewOptions = audioInterviewOptions?.Value ?? new AudioInterviewOptions();
            _emailService = emailService;
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

        // ============================================================
        // Candidate Audio Interview (Phase 2)
        // ============================================================

        public async Task<InterviewInviteResponseDto> CreateInviteAsync(
            int applicationId,
            InterviewInviteCreateDto? dto = null,
            CancellationToken cancellationToken = default)
        {
            var application = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.JobPosting)
                    .ThenInclude(j => j.QuestionBankItems)
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
                ?? throw new NotFoundException("Application", applicationId);

            var job = application.JobPosting
                ?? throw new BusinessRuleException("Không tìm thấy thông tin tin tuyển dụng gắn với hồ sơ ứng tuyển này.");

            if (job.Status != JobPostingStatus.Approved && job.Status != JobPostingStatus.Published)
            {
                throw new BusinessRuleException($"Chỉ có thể tạo phiên phỏng vấn khi tin tuyển dụng đã được duyệt (Approved hoặc Published). Trạng thái hiện tại: {job.Status}.");
            }

            var qbItems = job.QuestionBankItems
                .OrderBy(q => q.OrderIndex)
                .ToList();

            if (!qbItems.Any())
            {
                throw new BusinessRuleException("Tin tuyển dụng chưa có câu hỏi phỏng vấn trong Question Bank. Vui lòng tạo câu hỏi trước khi mời phỏng vấn.");
            }

            // Chuyển trạng thái Application theo state machine hợp lệ
            if (application.Status == ApplicationStatus.Applied)
            {
                application.TransitionTo(ApplicationStatus.Screening);
                application.TransitionTo(ApplicationStatus.Interviewing);
            }
            else if (application.Status == ApplicationStatus.Screening)
            {
                application.TransitionTo(ApplicationStatus.Interviewing);
            }
            else if (application.Status != ApplicationStatus.Interviewing)
            {
                throw new BusinessRuleException($"Không thể mời phỏng vấn hồ sơ đang ở trạng thái '{application.Status}'.");
            }

            var validityDays = dto?.ValidityDays ?? _audioInterviewOptions.DefaultInviteExpiryDays;
            if (validityDays <= 0) validityDays = 3;

            var interview = new Interview
            {
                ApplicationId = application.Id
            };
            interview.Invite(TimeSpan.FromDays(validityDays));

            // Copy câu hỏi từ QuestionBankItem sang InterviewQuestion: clone từng field của ScoringRubric (không gán reference)
            foreach (var qb in qbItems)
            {
                interview.Questions.Add(new InterviewQuestion
                {
                    Question = qb.Question,
                    OrderIndex = qb.OrderIndex,
                    ThinkSeconds = qb.ThinkSeconds,
                    AnswerSeconds = qb.AnswerSeconds,
                    ScoringRubric = qb.ScoringRubric != null
                        ? new ScoringRubric
                        {
                            Excellent = qb.ScoringRubric.Excellent,
                            Good = qb.ScoringRubric.Good,
                            Acceptable = qb.ScoringRubric.Acceptable,
                            Poor = qb.ScoringRubric.Poor
                        }
                        : null
                });
            }

            await _context.Interviews.AddAsync(interview, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var portalBase = _audioInterviewOptions.BaseCandidatePortalUrl.TrimEnd('/');
            var inviteUrl = $"{portalBase}/{interview.InviteToken}";

            _eventLogger.LogEvent("Interview",
                $"invited (ApplicationId: {application.Id}, Token: {interview.InviteToken}, ExpiresAt: {interview.InviteExpiresAt:yyyy-MM-dd HH:mm})",
                _eventLogger.OnEntityCreated);

            // Gửi email mời phỏng vấn (tùy chọn qua MailKit)
            if (dto?.SendEmail != false && !string.IsNullOrWhiteSpace(application.Candidate?.Email) && _emailService != null)
            {
                var candidateName = application.Candidate.FullName;
                var subject = $"[HRMS] Lời mời tham gia phỏng vấn vị trí {job.Title}";
                var body = $@"<p>Xin chào <strong>{candidateName}</strong>,</p>
<p>Bạn đã được mời tham gia vòng phỏng vấn ghi âm sơ loại cho vị trí <strong>{job.Title}</strong>.</p>
<p>Vui lòng truy cập liên kết dưới đây để bắt đầu bài phỏng vấn:</p>
<p><a href='{inviteUrl}' style='display:inline-block;padding:10px 20px;background-color:#2563eb;color:#ffffff;text-decoration:none;border-radius:6px;'>Bắt đầu phỏng vấn</a></p>
<p>Liên kết sẽ hết hạn vào lúc: <strong>{interview.InviteExpiresAt:dd/MM/yyyy HH:mm} (UTC)</strong>.</p>
<p>Trân trọng,<br/>Phòng Tuyển dụng</p>";

                try
                {
                    await _emailService.SendEmailAsync(application.Candidate.Email, subject, body);
                }
                catch
                {
                    // Email gửi lỗi không chặn luồng chính
                }
            }

            return new InterviewInviteResponseDto
            {
                InterviewId = interview.Id,
                ApplicationId = application.Id,
                InviteToken = interview.InviteToken,
                InviteUrl = inviteUrl,
                InviteExpiresAt = interview.InviteExpiresAt!.Value,
                ScheduledDate = interview.ScheduledDate,
                TotalQuestions = interview.Questions.Count,
                Status = interview.Status.ToString(),
                CandidateName = application.Candidate?.FullName ?? string.Empty,
                CandidateEmail = application.Candidate?.Email ?? string.Empty,
                JobTitle = job.Title
            };
        }

        public async Task<CandidateInterviewSessionResponseDto> GetCandidateSessionAsync(
            Guid inviteToken,
            CancellationToken cancellationToken = default)
        {
            var interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Application)
                    .ThenInclude(a => a.JobPosting)
                .Include(i => i.Questions)
                .Include(i => i.Answers)
                .FirstOrDefaultAsync(i => i.InviteToken == inviteToken, cancellationToken);

            if (interview == null)
            {
                throw new NotFoundException("Không tìm thấy phiên phỏng vấn với liên kết đã cung cấp.");
            }

            if (interview.Status == InterviewStatus.Cancelled)
            {
                throw new BusinessRuleException("Phiên phỏng vấn này đã bị hủy.", HttpStatusCode.BadRequest);
            }

            // Kiểm tra hết hạn: nếu quá hạn thì cập nhật lazy và trả 410 Gone
            if (interview.IsInviteExpired(DateTime.UtcNow) || interview.Status == InterviewStatus.Expired)
            {
                if (interview.Status != InterviewStatus.Expired)
                {
                    interview.MarkExpired();
                    await _context.SaveChangesAsync(cancellationToken);
                }
                throw new BusinessRuleException("Liên kết phỏng vấn này đã hết hạn.", HttpStatusCode.Gone);
            }

            // Đã hoàn thành: trả 409 Conflict
            if (interview.Status == InterviewStatus.Completed)
            {
                throw new ConflictException("Phiên phỏng vấn này đã hoàn thành.");
            }

            var answeredQuestionIds = interview.Answers
                .Select(a => a.InterviewQuestionId)
                .ToHashSet();

            // TUYỆT ĐỐI không trả ScoringRubric hay field nội bộ nào khác
            return new CandidateInterviewSessionResponseDto
            {
                InviteToken = interview.InviteToken,
                JobTitle = interview.Application?.JobPosting?.Title ?? "Phỏng vấn",
                CandidateName = interview.Application?.Candidate?.FullName ?? "Ứng viên",
                Status = interview.Status.ToString(),
                InviteExpiresAt = interview.InviteExpiresAt,
                ConsentedAt = interview.ConsentedAt,
                TotalQuestions = interview.Questions.Count,
                Questions = interview.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new CandidateQuestionDto
                    {
                        Id = q.Id,
                        Question = q.Question,
                        OrderIndex = q.OrderIndex,
                        ThinkSeconds = q.ThinkSeconds ?? _audioInterviewOptions.DefaultThinkSeconds,
                        AnswerSeconds = q.AnswerSeconds ?? _audioInterviewOptions.DefaultAnswerSeconds,
                        HasAnswer = answeredQuestionIds.Contains(q.Id)
                    })
                    .ToList()
            };
        }

        public async Task<CandidateInterviewStartResponseDto> StartCandidateInterviewAsync(
            Guid inviteToken,
            CancellationToken cancellationToken = default)
        {
            var interview = await _context.Interviews
                .FirstOrDefaultAsync(i => i.InviteToken == inviteToken, cancellationToken);

            if (interview == null)
            {
                throw new NotFoundException("Không tìm thấy phiên phỏng vấn với liên kết đã cung cấp.");
            }

            if (interview.Status == InterviewStatus.Cancelled)
            {
                throw new BusinessRuleException("Phiên phỏng vấn này đã bị hủy.", HttpStatusCode.BadRequest);
            }

            if (interview.IsInviteExpired(DateTime.UtcNow) || interview.Status == InterviewStatus.Expired)
            {
                if (interview.Status != InterviewStatus.Expired)
                {
                    interview.MarkExpired();
                    await _context.SaveChangesAsync(cancellationToken);
                }
                throw new BusinessRuleException("Liên kết phỏng vấn này đã hết hạn.", HttpStatusCode.Gone);
            }

            if (interview.Status == InterviewStatus.Completed)
            {
                throw new ConflictException("Phiên phỏng vấn này đã hoàn thành.");
            }

            // Idempotent: gọi lại khi đã InProgress sẽ giữ nguyên ConsentedAt
            interview.MarkInProgress();
            await _context.SaveChangesAsync(cancellationToken);

            _eventLogger.LogEvent("Interview",
                $"started (InterviewId: {interview.Id}, Token: {interview.InviteToken}, ConsentedAt: {interview.ConsentedAt:yyyy-MM-dd HH:mm})",
                _eventLogger.OnEntityUpdated);

            return new CandidateInterviewStartResponseDto
            {
                InviteToken = interview.InviteToken,
                Status = interview.Status.ToString(),
                ConsentedAt = interview.ConsentedAt,
                Message = "Bắt đầu phiên phỏng vấn thành công."
            };
        }
    }
}
