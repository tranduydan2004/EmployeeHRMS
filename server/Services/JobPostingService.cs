using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;
using EmployeeHRMS.Api.Services.Models;
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
        private readonly IJdGenerationService _jdGenerationService;
        private readonly IQuestionBankService _questionBankService;

        private static readonly Dictionary<string, Func<IQueryable<JobPosting>, bool, IOrderedQueryable<JobPosting>>> SortMappings =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = (q, desc) => desc ? q.OrderByDescending(j => j.Title) : q.OrderBy(j => j.Title),
                ["createddate"] = (q, desc) => desc ? q.OrderByDescending(j => j.CreatedDate) : q.OrderBy(j => j.CreatedDate),
                ["status"] = (q, desc) => desc ? q.OrderByDescending(j => j.Status) : q.OrderBy(j => j.Status),
            };

        public JobPostingService(
            AppDbContext context,
            EventLogger eventLogger,
            IJdGenerationService jdGenerationService,
            IQuestionBankService questionBankService)
        {
            _context = context;
            _eventLogger = eventLogger;
            _jdGenerationService = jdGenerationService;
            _questionBankService = questionBankService;
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

        // ============================================================
        // Phase 1: Smart JD & Question Bank Generation
        // ============================================================

        public async Task<JobPostingDetailResponseDto> DraftJdAsync(DraftJdRequestDto dto, string? createdBy = null)
        {
            var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
            if (!departmentExists)
                throw new BusinessRuleException($"Department with ID {dto.DepartmentId} does not exist.");

            // Lấy tên department cho prompt LLM
            var departmentName = await _context.Departments
                .Where(d => d.Id == dto.DepartmentId)
                .Select(d => d.Name)
                .FirstAsync();

            // Tạo entity JobPosting với tham số HR nhập
            var jobPosting = new JobPosting
            {
                Title = dto.Title,
                DepartmentId = dto.DepartmentId,
                Level = dto.Level,
                WorkMode = dto.WorkMode,
                CoreSkills = dto.CoreSkills,
                YearsOfExperience = dto.YearsOfExperience,
                SalaryRange = (dto.SalaryMin.HasValue || dto.SalaryMax.HasValue)
                    ? new SalaryRange
                    {
                        SalaryMin = dto.SalaryMin,
                        SalaryMax = dto.SalaryMax,
                        Currency = dto.Currency
                    }
                    : null,
                AdditionalNotes = dto.AdditionalNotes,
                Certifications = dto.Certifications,
                Status = JobPostingStatus.Draft,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };

            // Gọi LLM sinh bản thảo JD
            var promptData = new JdGenerationPromptData
            {
                JobTitle = dto.Title,
                DepartmentName = departmentName,
                Level = dto.Level,
                WorkMode = dto.WorkMode,
                CoreSkills = dto.CoreSkills,
                YearsOfExperience = dto.YearsOfExperience,
                SalaryMin = dto.SalaryMin,
                SalaryMax = dto.SalaryMax,
                Currency = dto.Currency,
                AdditionalNotes = dto.AdditionalNotes,
                Certifications = dto.Certifications
            };


            var jdContent = await _jdGenerationService.GenerateJdAsync(promptData);
            jobPosting.JdContent = jdContent;

            await _context.JobPostings.AddAsync(jobPosting);
            await _context.SaveChangesAsync();

            // Load Department cho projection
            await _context.Entry(jobPosting).Reference(j => j.Department).LoadAsync();

            _eventLogger.LogEvent("JobPosting", $"JD draft created (Title: {dto.Title})", _eventLogger.OnEntityCreated);

            return MapToDetailResponseDto(jobPosting);
        }

        public async Task<JobPostingDetailResponseDto> UpdateJdContentAsync(int id, UpdateJdContentDto dto)
        {
            var job = await _context.JobPostings
                .Include(j => j.Department)
                .Include(j => j.Applications)
                .Include(j => j.QuestionBankItems)
                .FirstOrDefaultAsync(j => j.Id == id)
                ?? throw new NotFoundException("JobPosting", id);

            if (job.Status != JobPostingStatus.Draft)
                throw new BusinessRuleException(
                    $"JD content can only be edited when status is Draft. Current status: {job.Status}.");

            // Cập nhật JdContent từ HR chỉnh sửa
            job.JdContent = new JdContent
            {
                Intro = dto.Intro,
                Responsibilities = dto.Responsibilities,
                MustHave = dto.MustHave,
                NiceToHave = dto.NiceToHave,
                Benefits = dto.Benefits
            };

            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("JobPosting", $"JD content updated (Id: {id})", _eventLogger.OnEntityUpdated);

            return MapToDetailResponseDto(job);
        }

        public async Task<JobPostingDetailResponseDto> ApproveJdAsync(int id)
        {
            var job = await _context.JobPostings
                .Include(j => j.Department)
                .Include(j => j.Applications)
                .Include(j => j.QuestionBankItems)
                .FirstOrDefaultAsync(j => j.Id == id)
                ?? throw new NotFoundException("JobPosting", id);

            if (job.Status != JobPostingStatus.Draft)
                throw new BusinessRuleException(
                    $"Only Draft job postings can be approved. Current status: {job.Status}.");

            if (job.JdContent == null)
                throw new BusinessRuleException(
                    "Cannot approve a job posting without JD content. Please generate or set JD content first.");

            // Chuyển trạng thái sang Approved
            job.Status = JobPostingStatus.Approved;
            job.ApprovedAt = DateTime.UtcNow;

            // Sinh Question Bank tự động khi Approve
            var promptData = new QuestionBankPromptData
            {
                JobTitle = job.Title,
                Level = job.Level,
                JdContent = job.JdContent
            };

            var generatedQuestions = await _questionBankService.GenerateQuestionsAsync(promptData);

            // Map kết quả LLM → QuestionBankItem entities và lưu DB
            var orderIndex = 0;
            foreach (var result in generatedQuestions)
            {
                var item = new QuestionBankItem
                {
                    JobPostingId = job.Id,
                    Question = result.Question,
                    Category = result.Category,
                    Difficulty = result.Difficulty,
                    ScoringRubric = result.ScoringRubric,
                    OrderIndex = orderIndex++,
                    CreatedDate = DateTime.UtcNow
                };
                job.QuestionBankItems.Add(item);
            }

            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("JobPosting",
                $"JD approved and {generatedQuestions.Count} questions generated (Id: {id})",
                _eventLogger.OnEntityUpdated);

            return MapToDetailResponseDto(job);
        }

        public async Task<JobPostingDetailResponseDto> GetDetailByIdAsync(int id)
        {
            var job = await _context.JobPostings
                .AsNoTracking()
                .Include(j => j.Department)
                .Include(j => j.Applications)
                .Include(j => j.QuestionBankItems)
                .FirstOrDefaultAsync(j => j.Id == id)
                ?? throw new NotFoundException("JobPosting", id);

            return MapToDetailResponseDto(job);
        }

        public async Task<List<QuestionBankItemResponseDto>> GetQuestionsAsync(int jobPostingId)
        {
            var jobExists = await _context.JobPostings.AnyAsync(j => j.Id == jobPostingId);
            if (!jobExists)
                throw new NotFoundException("JobPosting", jobPostingId);

            var questions = await _context.QuestionBankItems
                .AsNoTracking()
                .Where(q => q.JobPostingId == jobPostingId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();

            return questions.Select(MapToQuestionBankItemResponseDto).ToList();
        }

        // ============================================================
        // Private Helpers — Phase 1 Mapping
        // ============================================================

        private static JobPostingDetailResponseDto MapToDetailResponseDto(JobPosting job) => new()
        {
            Id = job.Id,
            Title = job.Title,
            Status = job.Status.ToString(),
            DepartmentName = job.Department?.Name ?? "N/A",
            DepartmentId = job.DepartmentId,
            Level = job.Level.ToString(),
            WorkMode = job.WorkMode.ToString(),
            CoreSkills = job.CoreSkills,
            YearsOfExperience = job.YearsOfExperience,
            SalaryRange = job.SalaryRange != null
                ? new SalaryRangeDto
                {
                    SalaryMin = job.SalaryRange.SalaryMin,
                    SalaryMax = job.SalaryRange.SalaryMax,
                    Currency = job.SalaryRange.Currency.ToString()
                }
                : null,
            AdditionalNotes = job.AdditionalNotes,
            Certifications = job.Certifications,
            JdContent = job.JdContent != null
                ? new JdContentDto
                {
                    Intro = job.JdContent.Intro,
                    Responsibilities = job.JdContent.Responsibilities,
                    MustHave = job.JdContent.MustHave,
                    NiceToHave = job.JdContent.NiceToHave,
                    Benefits = job.JdContent.Benefits
                }
                : null,
            CreatedBy = job.CreatedBy,
            CreatedDate = job.CreatedDate,
            ApprovedAt = job.ApprovedAt,
            ApplicationCount = job.Applications?.Count ?? 0,
            QuestionCount = job.QuestionBankItems?.Count ?? 0
        };

        private static QuestionBankItemResponseDto MapToQuestionBankItemResponseDto(QuestionBankItem q) => new()
        {
            Id = q.Id,
            JobPostingId = q.JobPostingId,
            Question = q.Question,
            Category = q.Category.ToString(),
            Difficulty = q.Difficulty.ToString(),
            ScoringRubric = q.ScoringRubric != null
                ? new ScoringRubricDto
                {
                    Excellent = q.ScoringRubric.Excellent,
                    Good = q.ScoringRubric.Good,
                    Acceptable = q.ScoringRubric.Acceptable,
                    Poor = q.ScoringRubric.Poor
                }
                : null,
            OrderIndex = q.OrderIndex,
            CreatedDate = q.CreatedDate
        };
    }
}
