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
    /// Department Service — EF Core implementation (PostgreSQL).
    /// Anemic model: logic CRUD nằm hoàn toàn ở Service layer.
    /// </summary>
    public class DepartmentService : IDepartmentService
    {
        private readonly AppDbContext _context;
        private readonly EventLogger _eventLogger;

        private static readonly Dictionary<string, Func<IQueryable<Department>, bool, IOrderedQueryable<Department>>> SortMappings =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = (q, desc) => desc ? q.OrderByDescending(d => d.Name) : q.OrderBy(d => d.Name),
                ["id"] = (q, desc) => desc ? q.OrderByDescending(d => d.Id) : q.OrderBy(d => d.Id),
            };

        public DepartmentService(AppDbContext context, EventLogger eventLogger)
        {
            _context = context;
            _eventLogger = eventLogger;
        }

        public async Task<PagedResult<DepartmentResponseDto>> GetAllAsync(PaginationParams paginationParams)
        {
            IQueryable<Department> query = _context.Departments
                .AsNoTracking()
                .Include(d => d.Employees);

            // ILike search theo Name (PostgreSQL case-insensitive, với fallback cho test)
            if (!string.IsNullOrWhiteSpace(paginationParams.Search))
            {
                var search = paginationParams.Search.Trim();
                if (_context.Database.IsNpgsql())
                {
                    query = query.Where(d => EF.Functions.ILike(d.Name, $"%{search}%"));
                }
                else
                {
                    query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()));
                }
            }

            var totalCount = await query.CountAsync();

            // Whitelist sorting — fallback mặc định "id" asc
            IOrderedQueryable<Department> orderedQuery;
            if (!string.IsNullOrWhiteSpace(paginationParams.SortBy) &&
                SortMappings.TryGetValue(paginationParams.SortBy, out var sortFunc))
            {
                orderedQuery = sortFunc(query, paginationParams.IsDescending);
            }
            else
            {
                orderedQuery = paginationParams.IsDescending
                    ? query.OrderByDescending(d => d.Id)
                    : query.OrderBy(d => d.Id);
            }

            var departments = await orderedQuery
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PagedResult<DepartmentResponseDto>
            {
                Items = departments.Select(MapToResponseDto).ToList(),
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }

        public async Task<PagedResult<DepartmentPublicDto>> GetAllPublicAsync(PaginationParams paginationParams)
        {
            // Truy vấn rút gọn không dùng Include(Employees) để tối ưu hiệu năng và bảo vệ dữ liệu nội bộ
            IQueryable<Department> query = _context.Departments.AsNoTracking();

            // ILike search theo Name (PostgreSQL case-insensitive, với fallback cho test)
            if (!string.IsNullOrWhiteSpace(paginationParams.Search))
            {
                var search = paginationParams.Search.Trim();
                if (_context.Database.IsNpgsql())
                {
                    query = query.Where(d => EF.Functions.ILike(d.Name, $"%{search}%"));
                }
                else
                {
                    query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()));
                }
            }

            var totalCount = await query.CountAsync();

            // Whitelist sorting — fallback mặc định "id" asc
            IOrderedQueryable<Department> orderedQuery;
            if (!string.IsNullOrWhiteSpace(paginationParams.SortBy) &&
                SortMappings.TryGetValue(paginationParams.SortBy, out var sortFunc))
            {
                orderedQuery = sortFunc(query, paginationParams.IsDescending);
            }
            else
            {
                orderedQuery = paginationParams.IsDescending
                    ? query.OrderByDescending(d => d.Id)
                    : query.OrderBy(d => d.Id);
            }

            var items = await orderedQuery
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .Select(d => new DepartmentPublicDto
                {
                    Id = d.Id,
                    Name = d.Name
                })
                .ToListAsync();

            return new PagedResult<DepartmentPublicDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }

        public async Task<DepartmentResponseDto?> GetByIdAsync(int id)
        {
            var department = await _context.Departments
                .AsNoTracking()
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new NotFoundException("Department", id);

            return MapToResponseDto(department);
        }

        public async Task<DepartmentResponseDto?> GetWithEmployeesAsync(int id)
        {
            var department = await _context.Departments
                .AsNoTracking()
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null) throw new NotFoundException("Department", id);

            // Projection kèm navigation data (Employees)
            return new DepartmentResponseDto
            {
                Id = department.Id,
                Name = department.Name,
                EmployeeCount = department.Employees.Count,
                EmployeeNames = department.Employees
                    .Select(e => e.FullName) // LINQ projection: Employee → string
                    .OrderBy(name => name)   // LINQ OrderBy
                    .ToList()
            };
        }

        public async Task<DepartmentResponseDto> CreateAsync(DepartmentCreateDto dto)
        {
            var department = new Department { Name = dto.Name };

            await _context.Departments.AddAsync(department);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Department", $"created (Name: {dto.Name})", _eventLogger.OnEntityCreated);

            return new DepartmentResponseDto
            {
                Id = department.Id,
                Name = department.Name,
                EmployeeCount = 0,
                EmployeeNames = new List<string>()
            };
        }

        public async Task<bool> UpdateAsync(int id, DepartmentUpdateDto dto)
        {
            var department = await _context.Departments.FindAsync(id)
                ?? throw new NotFoundException("Department", id);

            department.Name = dto.Name;
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Department", $"updated (Id: {id})", _eventLogger.OnEntityUpdated);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id)
                ?? throw new NotFoundException("Department", id);

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Department", $"deleted (Id: {id})", _eventLogger.OnEntityDeleted);
            return true;
        }

        // Private helper: map Department → DepartmentResponseDto
        private static DepartmentResponseDto MapToResponseDto(Department department)
        {
            return new DepartmentResponseDto
            {
                Id = department.Id,
                Name = department.Name,
                EmployeeCount = department.Employees.Count,
                EmployeeNames = department.Employees.Select(e => e.FullName).OrderBy(n => n).ToList()
            };
        }
    }
}
