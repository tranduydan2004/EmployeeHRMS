using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// JobPosting Service — EF Core implementation (PostgreSQL).
    /// Anemic model: logic CRUD nằm hoàn toàn ở Service layer.
    /// Minh họa: LINQ Where/Select, async/await với EF Core.
    /// </summary>
    public class JobPostingService : IJobPostingService
    {
        private readonly AppDbContext _context;
        private readonly EventLogger _eventLogger;

        private static readonly Dictionary<string, Func<IQueryable<JobPosting>, bool, IOrderedQueryable<JobPosting>>> SortMappings =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = (q, desc) => desc ? q.OrderByDescending(j => j.Title) : q.OrderBy(j => j.Title),
                ["createddate"] = (q, desc) => desc ? q.OrderByDescending(j => j.CreatedDate) : q.OrderBy(j => j.CreatedDate),
                ["status"] = (q, desc) => desc ? q.OrderByDescending(j => j.Status) : q.OrderBy(j => j.Status),
            };

        public JobPostingService(AppDbContext context, EventLogger eventLogger)
        {
            _context = context;
            _eventLogger = eventLogger;
        }

        // Private helper: map JobPosting entity → JobPostingResponseDto (dùng sau khi đã Include)
        private static JobPostingResponseDto MapToResponseDto(JobPosting job) => new()
        {
            Id = job.Id,
            Title = job.Title,
            Description = job.Description,
            Requirements = job.Requirements,
            Status = job.Status.ToString(),
            DepartmentName = job.Department?.Name ?? "N/A",
            ApplicationCount = job.Applications.Count,
            CreatedDate = job.CreatedDate
        };

        public async Task<PagedResult<JobPostingResponseDto>> GetAllAsync(JobPostingQueryParams queryParams)
        {
            IQueryable<JobPosting> query = _context.JobPostings
                .AsNoTracking()
                .Include(j => j.Department)
                .Include(j => j.Applications);

            // Filter theo Status nếu có
            if (queryParams.Status.HasValue)
            {
                query = query.Where(j => j.Status == queryParams.Status.Value);
            }

            // Filter theo DepartmentId nếu có
            if (queryParams.DepartmentId.HasValue)
            {
                query = query.Where(j => j.DepartmentId == queryParams.DepartmentId.Value);
            }

            // ILike search theo Title (PostgreSQL case-insensitive, với fallback cho test)
            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search.Trim();
                if (_context.Database.IsNpgsql())
                {
                    query = query.Where(j => EF.Functions.ILike(j.Title, $"%{search}%"));
                }
                else
                {
                    query = query.Where(j => j.Title.ToLower().Contains(search.ToLower()));
                }
            }

            var totalCount = await query.CountAsync();

            // Whitelist sorting — fallback mặc định "createddate" desc
            IOrderedQueryable<JobPosting> orderedQuery;
            if (!string.IsNullOrWhiteSpace(queryParams.SortBy) &&
                SortMappings.TryGetValue(queryParams.SortBy, out var sortFunc))
            {
                orderedQuery = sortFunc(query, queryParams.IsDescending);
            }
            else
            {
                orderedQuery = queryParams.IsDescending
                    ? query.OrderByDescending(j => j.CreatedDate)
                    : query.OrderByDescending(j => j.CreatedDate);
            }

            var jobs = await orderedQuery
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return new PagedResult<JobPostingResponseDto>
            {
                Items = jobs.Select(MapToResponseDto).ToList(),
                TotalCount = totalCount,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize
            };
        }

        public async Task<JobPostingResponseDto?> GetByIdAsync(int id)
        {
            var job = await _context.JobPostings
                .AsNoTracking()
                .Include(j => j.Department)
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == id)
                ?? throw new NotFoundException("JobPosting", id);

            return MapToResponseDto(job);
        }

        /// <summary>
        /// Lấy danh sách tin tuyển dụng đang Published.
        /// Minh họa: LINQ Where với enum comparison trên EF Core.
        /// </summary>
        public async Task<IEnumerable<JobPostingResponseDto>> GetActiveJobsAsync()
        {
            var jobs = await _context.JobPostings
                .AsNoTracking()
                .Include(j => j.Department)
                .Include(j => j.Applications)
                .Where(j => j.Status == JobPostingStatus.Published)
                .ToListAsync();

            return jobs.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<JobPostingResponseDto>> GetByDepartmentAsync(int departmentId)
        {
            var jobs = await _context.JobPostings
                .AsNoTracking()
                .Include(j => j.Department)
                .Include(j => j.Applications)
                .Where(j => j.DepartmentId == departmentId)
                .ToListAsync();

            return jobs.Select(MapToResponseDto);
        }

        public async Task<JobPostingResponseDto> CreateAsync(JobPostingCreateDto dto)
        {
            // Kiểm tra DepartmentId tồn tại (ràng buộc FK)
            var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
            if (!departmentExists)
                throw new BusinessRuleException($"Department with ID {dto.DepartmentId} does not exist.");

            var jobPosting = new JobPosting
            {
                Title = dto.Title,
                Description = dto.Description,
                Requirements = dto.Requirements,
                DepartmentId = dto.DepartmentId,
                Status = dto.Status ?? JobPostingStatus.Draft,
                CreatedDate = DateTime.UtcNow
            };

            await _context.JobPostings.AddAsync(jobPosting);
            await _context.SaveChangesAsync();

            // Load Department cho projection
            await _context.Entry(jobPosting).Reference(j => j.Department).LoadAsync();

            _eventLogger.LogEvent("JobPosting", $"created (Title: {dto.Title})", _eventLogger.OnEntityCreated);

            return MapToResponseDto(jobPosting);
        }

        public async Task<bool> UpdateAsync(int id, JobPostingUpdateDto dto)
        {
            var job = await _context.JobPostings.FindAsync(id)
                ?? throw new NotFoundException("JobPosting", id);

            // Kiểm tra DepartmentId tồn tại (ràng buộc FK)
            var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
            if (!departmentExists)
                throw new BusinessRuleException($"Department with ID {dto.DepartmentId} does not exist.");

            job.Title = dto.Title;
            job.Description = dto.Description;
            job.Requirements = dto.Requirements;
            job.DepartmentId = dto.DepartmentId;
            job.Status = dto.Status;

            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("JobPosting", $"updated (Id: {id})", _eventLogger.OnEntityUpdated);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var job = await _context.JobPostings.FindAsync(id)
                ?? throw new NotFoundException("JobPosting", id);

            _context.JobPostings.Remove(job);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("JobPosting", $"deleted (Id: {id})", _eventLogger.OnEntityDeleted);
            return true;
        }
    }
}
