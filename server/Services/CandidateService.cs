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
    /// Candidate Service — EF Core implementation (PostgreSQL).
    /// Anemic model: logic CRUD nằm hoàn toàn ở Service layer.
    /// GetAllAsync: lọc dữ liệu tự động theo Role (Admin/HR xem hết, Candidate chỉ thấy mình).
    /// </summary>
    public class CandidateService : ICandidateService
    {
        private readonly AppDbContext _context;
        private readonly EventLogger _eventLogger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Whitelist sorting
        private static readonly Dictionary<string, Func<IQueryable<Candidate>, bool, IOrderedQueryable<Candidate>>>
            SortMappings = new(StringComparer.OrdinalIgnoreCase)
            {
                ["fullname"] = (q, desc) => desc ? q.OrderByDescending(c => c.FullName) : q.OrderBy(c => c.FullName),
                ["email"] = (q, desc) => desc ? q.OrderByDescending(c => c.Email) : q.OrderBy(c => c.Email),
                ["phone"] = (q, desc) => desc ? q.OrderByDescending(c => c.Phone) : q.OrderBy(c => c.Phone),
            };

        public CandidateService(AppDbContext context, EventLogger eventLogger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _eventLogger = eventLogger;
            _httpContextAccessor = httpContextAccessor;
        }

        // Private helper: map Candidate entity → CandidateResponseDto
        private static CandidateResponseDto MapToResponseDto(Candidate c) => new()
        {
            Id = c.Id,
            FullName = c.FullName,
            Email = c.Email,
            Phone = c.Phone,
            ResumeUrl = c.ResumeUrl,
            ApplicationCount = c.Applications.Count // Computed field từ navigation property
        };

        public async Task<PagedResult<CandidateResponseDto>> GetAllAsync(PaginationParams paginationParams)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var role = user?.GetRole();
            var currentUserId = user?.GetUserId();

            IQueryable<Candidate> query = _context.Candidates
                .AsNoTracking()
                .Include(c => c.Applications);

            // Layer A: Filter theo Role TRƯỚC
            if (role == nameof(UserRole.Candidate) && currentUserId.HasValue)
            {
                query = query.Where(c => c.UserId.HasValue && c.UserId.Value == currentUserId.Value);
            }
            else if (role == nameof(UserRole.Interviewer) && currentUserId.HasValue)
            {
                query = query.Where(c => c.Applications.Any(a =>
                    _context.Interviews.Any(i =>
                        i.ApplicationId == a.Id && i.InterviewerId == currentUserId.Value)));
            }

            // ILike search theo FullName, Email (sau Role filter và trước CountAsync)
            if (!string.IsNullOrWhiteSpace(paginationParams.Search))
            {
                var search = paginationParams.Search.Trim();
                if (_context.Database.IsNpgsql())
                {
                    query = query.Where(c =>
                        EF.Functions.ILike(c.FullName, $"%{search}%") ||
                        EF.Functions.ILike(c.Email, $"%{search}%"));
                }
                else
                {
                    query = query.Where(c =>
                        c.FullName.ToLower().Contains(search.ToLower()) ||
                        c.Email.ToLower().Contains(search.ToLower()));
                }
            }

            // Count trên tập ĐÃ FILTER + SEARCH (không phải toàn bảng)
            var totalCount = await query.CountAsync();

            // Whitelist sorting — fallback mặc định theo Id
            IOrderedQueryable<Candidate> orderedQuery;
            if (!string.IsNullOrWhiteSpace(paginationParams.SortBy) &&
                SortMappings.TryGetValue(paginationParams.SortBy, out var sortFunc))
            {
                orderedQuery = sortFunc(query, paginationParams.IsDescending);
            }
            else
            {
                orderedQuery = paginationParams.IsDescending
                    ? query.OrderByDescending(c => c.Id)
                    : query.OrderBy(c => c.Id);
            }

            var candidates = await orderedQuery
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PagedResult<CandidateResponseDto>
            {
                Items = candidates.Select(MapToResponseDto).ToList(),
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }

        public async Task<CandidateResponseDto?> GetByIdAsync(int id)
        {
            var candidate = await _context.Candidates
                .AsNoTracking()
                .Include(c => c.Applications)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new NotFoundException("Candidate", id);

            return MapToResponseDto(candidate);
        }

        /// <summary>
        /// Tìm kiếm ứng viên theo keyword — search trong FullName, Email, Phone.
        /// Dùng ToLower().Contains() để case-insensitive (Npgsql dịch sang LOWER() trong SQL).
        /// </summary>
        public async Task<IEnumerable<CandidateResponseDto>> SearchAsync(string keyword)
        {
            var lowerKeyword = keyword.ToLower();

            var candidates = await _context.Candidates
                .AsNoTracking()
                .Include(c => c.Applications)
                .Where(c =>
                    c.FullName.ToLower().Contains(lowerKeyword) ||
                    c.Email.ToLower().Contains(lowerKeyword) ||
                    c.Phone.Contains(keyword))
                .ToListAsync();

            return candidates.Select(MapToResponseDto);
        }

        public async Task<CandidateResponseDto> CreateAsync(CandidateCreateDto dto)
        {
            // P0 Security Fix: Lấy UserId từ JWT claims — TUYỆT ĐỐI KHÔNG đọc từ DTO
            var currentUserId = _httpContextAccessor.HttpContext?.User?.GetUserId();

            // Kiểm tra duplicate: mỗi user chỉ được tạo 1 hồ sơ ứng viên
            if (currentUserId.HasValue)
            {
                var alreadyExists = await _context.Candidates
                    .AnyAsync(c => c.UserId == currentUserId.Value);
                if (alreadyExists)
                    throw new BusinessRuleException("Bạn đã tạo hồ sơ ứng viên rồi.");
            }

            var candidate = new Candidate
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                ResumeUrl = dto.ResumeUrl ?? string.Empty,
                UserId = currentUserId // Gán UserId tự động từ HttpContext
            };

            await _context.Candidates.AddAsync(candidate);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Candidate", $"created (Name: {dto.FullName})", _eventLogger.OnEntityCreated);

            return new CandidateResponseDto
            {
                Id = candidate.Id,
                FullName = candidate.FullName,
                Email = candidate.Email,
                Phone = candidate.Phone,
                ResumeUrl = candidate.ResumeUrl,
                ApplicationCount = 0
            };
        }

        public async Task<bool> UpdateAsync(int id, CandidateUpdateDto dto)
        {
            var candidate = await _context.Candidates.FindAsync(id)
                ?? throw new NotFoundException("Candidate", id);

            // Anemic model: set properties trực tiếp
            if (!string.IsNullOrWhiteSpace(dto.FullName))
                candidate.FullName = dto.FullName;
            if (!string.IsNullOrWhiteSpace(dto.Email))
                candidate.Email = dto.Email;
            if (!string.IsNullOrWhiteSpace(dto.Phone))
                candidate.Phone = dto.Phone;
            if (!string.IsNullOrWhiteSpace(dto.ResumeUrl))
                candidate.ResumeUrl = dto.ResumeUrl;

            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Candidate", $"updated (Id: {id})", _eventLogger.OnEntityUpdated);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var candidate = await _context.Candidates.FindAsync(id)
                ?? throw new NotFoundException("Candidate", id);

            _context.Candidates.Remove(candidate);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Candidate", $"deleted (Id: {id})", _eventLogger.OnEntityDeleted);
            return true;
        }

        /// <summary>
        /// Fetch Candidate entity cho authorization handler (không DTO).
        /// </summary>
        public async Task<Candidate?> GetEntityByIdAsync(int id)
        {
            return await _context.Candidates
                .Include(c => c.Applications)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
