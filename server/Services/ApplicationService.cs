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
    /// Application Service — EF Core implementation (PostgreSQL).
    /// DDD model: nghiệp vụ chuyển trạng thái nằm trong entity (Application.TransitionTo()).
    /// GetAllAsync: lọc dữ liệu tự động theo Role.
    /// </summary>
    public class ApplicationService : IApplicationService
    {
        private readonly AppDbContext _context;
        private readonly EventLogger _eventLogger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Whitelist sorting
        private static readonly Dictionary<string, Func<IQueryable<Application>, bool, IOrderedQueryable<Application>>>
            SortMappings = new(StringComparer.OrdinalIgnoreCase)
            {
                ["applieddate"] = (q, desc) => desc ? q.OrderByDescending(a => a.AppliedDate) : q.OrderBy(a => a.AppliedDate),
                ["status"] = (q, desc) => desc ? q.OrderByDescending(a => a.Status) : q.OrderBy(a => a.Status),
                ["candidatename"] = (q, desc) => desc ? q.OrderByDescending(a => a.Candidate.FullName) : q.OrderBy(a => a.Candidate.FullName),
                ["jobtitle"] = (q, desc) => desc ? q.OrderByDescending(a => a.JobPosting.Title) : q.OrderBy(a => a.JobPosting.Title),
            };

        public ApplicationService(AppDbContext context, EventLogger eventLogger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _eventLogger = eventLogger;
            _httpContextAccessor = httpContextAccessor;
        }

        // Private helper: map Application entity → ApplicationResponseDto (sau khi đã Include Candidate + JobPosting)
        private static ApplicationResponseDto MapToResponseDto(Application app) => new()
        {
            Id = app.Id,
            AppliedDate = app.AppliedDate,
            Status = app.Status.ToString(),
            CandidateName = app.Candidate?.FullName ?? "Unknown",
            JobTitle = app.JobPosting?.Title ?? "Unknown",
            AiMatchScore = app.AiMatchScore,
            AiSummary = app.AiSummary,
            AiPros = app.AiPros,
            AiCons = app.AiCons
        };

        public async Task<PagedResult<ApplicationResponseDto>> GetAllAsync(PaginationParams paginationParams)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var role = user?.GetRole();
            var currentUserId = user?.GetUserId();

            IQueryable<Application> query = _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.JobPosting);

            // Layer A: Filter theo Role TRƯỚC
            if (role == nameof(UserRole.Candidate) && currentUserId.HasValue)
            {
                query = query.Where(a => a.Candidate.UserId.HasValue &&
                    a.Candidate.UserId.Value == currentUserId.Value);
            }
            else if (role == nameof(UserRole.Interviewer) && currentUserId.HasValue)
            {
                query = query.Where(a =>
                    _context.Interviews.Any(i =>
                        i.ApplicationId == a.Id && i.InterviewerId == currentUserId.Value));
            }

            // ILike search theo Candidate FullName/Email hoặc Job Title
            if (!string.IsNullOrWhiteSpace(paginationParams.Search))
            {
                var search = paginationParams.Search.Trim();
                if (_context.Database.IsNpgsql())
                {
                    query = query.Where(a =>
                        EF.Functions.ILike(a.Candidate.FullName, $"%{search}%") ||
                        EF.Functions.ILike(a.Candidate.Email, $"%{search}%") ||
                        EF.Functions.ILike(a.JobPosting.Title, $"%{search}%"));
                }
                else
                {
                    query = query.Where(a =>
                        a.Candidate.FullName.ToLower().Contains(search.ToLower()) ||
                        a.Candidate.Email.ToLower().Contains(search.ToLower()) ||
                        a.JobPosting.Title.ToLower().Contains(search.ToLower()));
                }
            }

            // Count trên tập ĐÃ FILTER + SEARCH
            var totalCount = await query.CountAsync();

            // Whitelist sorting
            IOrderedQueryable<Application> orderedQuery;
            if (!string.IsNullOrWhiteSpace(paginationParams.SortBy) &&
                SortMappings.TryGetValue(paginationParams.SortBy, out var sortFunc))
            {
                orderedQuery = sortFunc(query, paginationParams.IsDescending);
            }
            else
            {
                orderedQuery = paginationParams.IsDescending
                    ? query.OrderByDescending(a => a.Id)
                    : query.OrderBy(a => a.Id);
            }

            var applications = await orderedQuery
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PagedResult<ApplicationResponseDto>
            {
                Items = applications.Select(MapToResponseDto).ToList(),
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }

        public async Task<ApplicationResponseDto?> GetByIdAsync(int id)
        {
            var app = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.JobPosting)
                .FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new NotFoundException("Application", id);

            return MapToResponseDto(app);
        }

        /// <summary>
        /// Lọc đơn ứng tuyển theo trạng thái.
        /// Minh họa: LINQ Where với enum filter trên EF Core.
        /// </summary>
        public async Task<IEnumerable<ApplicationResponseDto>> GetByStatusAsync(ApplicationStatus status)
        {
            var applications = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.JobPosting)
                .Where(a => a.Status == status)
                .ToListAsync();

            return applications.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ApplicationResponseDto>> GetByCandidateAsync(int candidateId)
        {
            var applications = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.JobPosting)
                .Where(a => a.CandidateId == candidateId)
                .ToListAsync();

            return applications.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<ApplicationResponseDto>> GetByJobPostingAsync(int jobPostingId)
        {
            var applications = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.JobPosting)
                .Where(a => a.JobPostingId == jobPostingId)
                .ToListAsync();

            return applications.Select(MapToResponseDto);
        }

        public async Task<ApplicationResponseDto> CreateAsync(ApplicationCreateDto dto)
        {
            // Kiểm tra ứng viên đã ứng tuyển vị trí này chưa (LINQ AnyAsync)
            var alreadyApplied = await _context.Applications
                .AnyAsync(a => a.CandidateId == dto.CandidateId && a.JobPostingId == dto.JobPostingId);

            if (alreadyApplied)
                throw new BusinessRuleException("Bạn đã nộp đơn ứng tuyển cho vị trí này rồi.", System.Net.HttpStatusCode.Conflict);

            // Kiểm tra CandidateId tồn tại
            var candidateExists = await _context.Candidates.AnyAsync(c => c.Id == dto.CandidateId);
            if (!candidateExists)
                throw new BusinessRuleException($"Candidate with ID {dto.CandidateId} does not exist.");

            // Kiểm tra JobPostingId tồn tại
            var jobExists = await _context.JobPostings.AnyAsync(j => j.Id == dto.JobPostingId);
            if (!jobExists)
                throw new BusinessRuleException($"Job Posting with ID {dto.JobPostingId} does not exist.");

            var application = new Application
            {
                CandidateId = dto.CandidateId,
                JobPostingId = dto.JobPostingId,
                AppliedDate = DateTime.UtcNow
            };
            application.SetInitialStatus(); // DDD domain method — set Status = Applied

            await _context.Applications.AddAsync(application);
            await _context.SaveChangesAsync();

            // Load navigation properties cho projection
            await _context.Entry(application).Reference(a => a.Candidate).LoadAsync();
            await _context.Entry(application).Reference(a => a.JobPosting).LoadAsync();

            _eventLogger.LogEvent("Application",
                $"created (CandidateId: {dto.CandidateId}, JobId: {dto.JobPostingId})",
                _eventLogger.OnEntityCreated);

            return MapToResponseDto(application);
        }

        /// <summary>
        /// Nghiệp vụ chuyển trạng thái đơn ứng tuyển — gọi DDD domain method.
        /// Entity Application.TransitionTo() tự validate luồng trạng thái.
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int id, ApplicationStatus newStatus)
        {
            var application = await _context.Applications.FindAsync(id)
                ?? throw new NotFoundException("Application", id);

            // DDD domain method — validate + set status
            application.TransitionTo(newStatus);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Application",
                $"status changed to {newStatus} (Id: {id})",
                _eventLogger.OnEntityUpdated);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var application = await _context.Applications.FindAsync(id)
                ?? throw new NotFoundException("Application", id);

            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Application", $"deleted (Id: {id})", _eventLogger.OnEntityDeleted);
            return true;
        }

        /// <summary>
        /// Fetch Application entity cho authorization handler (không DTO).
        /// </summary>
        public async Task<Application?> GetEntityByIdAsync(int id)
        {
            return await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.JobPosting)
                .FirstOrDefaultAsync(a => a.Id == id);
        }
    }
}
