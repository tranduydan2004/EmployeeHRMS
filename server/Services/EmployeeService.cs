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
    /// Employee Service — EF Core implementation (PostgreSQL).
    /// Anemic model: logic CRUD nằm hoàn toàn ở Service layer.
    /// Minh họa: LINQ nặng (GroupBy, projection, aggregate), async/await,
    /// Func delegate, extension methods, Action delegate (EventLogger).
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;
        private readonly EventLogger _eventLogger;
        private readonly IEmailService _emailService;
        private readonly UserProvisioningService _provisioningService;
        private readonly ILogger<EmployeeService> _logger;

        // Whitelist sorting — map SortBy string → Expression (an toàn, không dynamic string injection)
        private static readonly Dictionary<string, Func<IQueryable<Employee>, bool, IOrderedQueryable<Employee>>>
            SortMappings = new(StringComparer.OrdinalIgnoreCase)
            {
                ["fullname"] = (q, desc) => desc ? q.OrderByDescending(e => e.FullName) : q.OrderBy(e => e.FullName),
                ["email"] = (q, desc) => desc ? q.OrderByDescending(e => e.Email) : q.OrderBy(e => e.Email),
                ["salary"] = (q, desc) => desc ? q.OrderByDescending(e => e.Salary) : q.OrderBy(e => e.Salary),
                ["joindate"] = (q, desc) => desc ? q.OrderByDescending(e => e.JoinDate) : q.OrderBy(e => e.JoinDate),
                ["position"] = (q, desc) => desc ? q.OrderByDescending(e => e.Position) : q.OrderBy(e => e.Position),
                ["status"] = (q, desc) => desc ? q.OrderByDescending(e => e.Status) : q.OrderBy(e => e.Status),
            };

        // === Constructor injection (DI) ===
        public EmployeeService(
            AppDbContext context,
            EventLogger eventLogger,
            IEmailService emailService,
            UserProvisioningService provisioningService,
            ILogger<EmployeeService> logger)
        {
            _context = context;
            _eventLogger = eventLogger;
            _emailService = emailService;
            _provisioningService = provisioningService;
            _logger = logger;
        }

        // Private helper: map Employee entity → EmployeeResponseDto (dùng sau khi đã Include Department)
        private static EmployeeResponseDto MapToResponseDto(Employee e) => new()
        {
            EmployeeId = e.EmployeeId,
            FullName = e.FullName,
            Email = e.Email,
            Position = e.Position,
            JoinDate = e.JoinDate,
            Salary = e.Salary,
            Status = e.Status.ToString(),
            DepartmentName = e.Department?.Name ?? "N/A",
            CreatedAt = e.CreatedAt
        };

        // ============================================================
        // READ OPERATIONS — EF Core + LINQ + Async
        // ============================================================

        public async Task<PagedResult<EmployeeResponseDto>> GetAllAsync(PaginationParams paginationParams)
        {
            IQueryable<Employee> query = _context.Employees
                .AsNoTracking()
                .Include(e => e.Department);

            // ILike search theo FullName, Email, Position (PostgreSQL case-insensitive, fallback cho in-memory)
            if (!string.IsNullOrWhiteSpace(paginationParams.Search))
            {
                var search = paginationParams.Search.Trim();
                if (_context.Database.IsNpgsql())
                {
                    query = query.Where(e =>
                        EF.Functions.ILike(e.FullName, $"%{search}%") ||
                        EF.Functions.ILike(e.Email, $"%{search}%") ||
                        EF.Functions.ILike(e.Position, $"%{search}%"));
                }
                else
                {
                    query = query.Where(e =>
                        e.FullName.ToLower().Contains(search.ToLower()) ||
                        e.Email.ToLower().Contains(search.ToLower()) ||
                        e.Position.ToLower().Contains(search.ToLower()));
                }
            }

            // Count trước khi phân trang (trên tập đã search)
            var totalCount = await query.CountAsync();

            // Whitelist sorting — fallback mặc định theo EmployeeId
            IOrderedQueryable<Employee> orderedQuery;
            if (!string.IsNullOrWhiteSpace(paginationParams.SortBy) &&
                SortMappings.TryGetValue(paginationParams.SortBy, out var sortFunc))
            {
                orderedQuery = sortFunc(query, paginationParams.IsDescending);
            }
            else
            {
                orderedQuery = paginationParams.IsDescending
                    ? query.OrderByDescending(e => e.EmployeeId)
                    : query.OrderBy(e => e.EmployeeId);
            }

            var employees = await orderedQuery
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PagedResult<EmployeeResponseDto>
            {
                Items = employees.Select(MapToResponseDto).ToList(),
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }

        public async Task<EmployeeResponseDto?> GetByIdAsync(int employeeId)
        {
            var employee = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId)
                ?? throw new NotFoundException("Employee", employeeId);

            return MapToResponseDto(employee);
        }

        public async Task<Employee?> GetEntityByIdAsync(int employeeId)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        }

        public async Task<IEnumerable<EmployeeResponseDto>> GetByStatusAsync(EmployeeStatus status)
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Where(e => e.Status == status)
                .ToListAsync();

            return employees.Select(MapToResponseDto);
        }

        public async Task<EmployeeResponseDto> GetMeAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new NotFoundException("User", userId);

            if (!user.EmployeeId.HasValue)
                throw new BusinessRuleException("Tài khoản này chưa được liên kết với hồ sơ nhân viên.");

            return await GetByIdAsync(user.EmployeeId.Value)
                ?? throw new NotFoundException("Employee", user.EmployeeId.Value);
        }

        public async Task<IEnumerable<EmployeeColleagueDto>> GetMyDepartmentColleaguesAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new NotFoundException("User", userId);

            if (!user.EmployeeId.HasValue)
                throw new BusinessRuleException("Tài khoản này chưa được liên kết với hồ sơ nhân viên.");

            var currentEmployee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == user.EmployeeId.Value)
                ?? throw new NotFoundException("Employee", user.EmployeeId.Value);

            var colleagues = await _context.Employees
                .AsNoTracking()
                .Where(e => e.DepartmentId == currentEmployee.DepartmentId && e.EmployeeId != currentEmployee.EmployeeId)
                .Select(e => new EmployeeColleagueDto
                {
                    Id = e.EmployeeId,
                    FullName = e.FullName,
                    Position = e.Position,
                    Email = e.Email
                })
                .ToListAsync();

            return colleagues;
        }

        /// <summary>
        /// Search bằng Func&lt;Employee, bool&gt; delegate — caller truyền bất kỳ filter logic nào.
        /// Lưu ý: Func delegate không thể dịch sang SQL, nên phải load toàn bộ rồi filter client-side.
        /// </summary>
        public async Task<IEnumerable<EmployeeResponseDto>> SearchAsync(Func<Employee, bool> predicate)
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .ToListAsync();

            return employees
                .Where(predicate)
                .Select(MapToResponseDto);
        }

        public async Task<IEnumerable<EmployeeResponseDto>> FindByNameAsync(string name)
        {
            // Dùng EF Core Where trực tiếp (dịch sang SQL) thay vì gọi SearchAsync
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Where(e => e.FullName.ToLower().Contains(name.ToLower()))
                .ToListAsync();

            return employees.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<EmployeeResponseDto>> FindByDepartmentAsync(int departmentId)
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Where(e => e.DepartmentId == departmentId)
                .ToListAsync();

            return employees.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<EmployeeResponseDto>> GetTopSalaryAsync(int count = 10)
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .OrderByDescending(e => e.Salary)
                .Take(count)
                .ToListAsync();

            return employees.Select(MapToResponseDto);
        }

        /// <summary>
        /// Thống kê nhân viên theo phòng ban.
        /// Minh họa: LINQ GroupBy + Aggregate functions (Count, Average, Max, Min) + Projection.
        /// </summary>
        public async Task<IEnumerable<EmployeeStatisticsDto>> GetStatisticsByDepartmentAsync()
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .ToListAsync();

            return employees
                .GroupBy(e => e.Department?.Name ?? "Unknown")
                .Select(group => new EmployeeStatisticsDto
                {
                    DepartmentName = group.Key,
                    EmployeeCount = group.Count(),
                    AverageSalary = group.Average(e => e.Salary),
                    MaxSalary = group.Max(e => e.Salary),
                    MinSalary = group.Min(e => e.Salary)
                })
                .OrderByDescending(s => s.EmployeeCount)
                .ToList();
        }

        // ============================================================
        // WRITE OPERATIONS — EF Core async (AddAsync, SaveChangesAsync)
        // ============================================================

        public async Task<EmployeeCreateResponseDto> CreateAsync(EmployeeCreateDto dto)
        {
            // Kiểm tra DepartmentId tồn tại (ràng buộc FK)
            var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
            if (!departmentExists)
                throw new BusinessRuleException($"Department with ID {dto.DepartmentId} does not exist.");

            ProvisionResult? provisionResult = null;
            Employee employee;

            // === Transaction: CHỈ các thao tác ghi DB ===
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo Employee
                employee = new Employee
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Position = dto.Position,
                    JoinDate = dto.JoinDate,
                    DepartmentId = dto.DepartmentId,
                    Salary = dto.Salary,
                    CreatedAt = DateTime.UtcNow,
                    Status = EmployeeStatus.Active
                };

                await _context.Employees.AddAsync(employee);
                await _context.SaveChangesAsync();

                // 2. Xử lý SourceApplicationId (nếu có)
                if (dto.SourceApplicationId.HasValue)
                {
                    var application = await _context.Applications
                        .Include(a => a.Candidate)
                        .FirstOrDefaultAsync(a => a.Id == dto.SourceApplicationId.Value)
                        ?? throw new BusinessRuleException(
                            $"Application with ID {dto.SourceApplicationId.Value} does not exist.");

                    // Cross-check: email của Candidate phải khớp với dto.Email
                    if (!string.Equals(application.Candidate.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new BusinessRuleException(
                            "SourceApplicationId không thuộc về Email đang tạo Employee.");
                    }

                    // Kiểm tra trạng thái Application phải là Interviewing
                    if (application.Status != ApplicationStatus.Interviewing)
                    {
                        throw new BusinessRuleException(
                            $"Application phải ở trạng thái Interviewing để chuyển sang Offered. " +
                            $"Trạng thái hiện tại: {application.Status}.");
                    }

                    // Bypass State Machine: luồng Onboarding nội bộ,
                    // KHÔNG áp dụng TransitionTo() của public API
                    application.TransitionTo(ApplicationStatus.Offered);
                    await _context.SaveChangesAsync();
                }

                // 3. Provision tài khoản Employee
                provisionResult = await _provisioningService
                    .ProvisionEmployeeAccountAsync(dto.Email, employee.EmployeeId);

                // 4. Commit transaction
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // === SAU COMMIT: Gửi email (ngoài transaction) ===
            var emailSent = true;

            if (provisionResult.Kind == "NewAccount")
            {
                emailSent = await _emailService.SendEmailAsync(
                    dto.Email,
                    "[HRMS] Thông tin đăng nhập tài khoản nhân viên",
                    $"<h2>Chào mừng bạn đến với HRMS!</h2>" +
                    $"<p>Tài khoản của bạn đã được tạo:</p>" +
                    $"<p><strong>Email:</strong> {dto.Email}</p>" +
                    $"<p><strong>Mật khẩu:</strong> {provisionResult.PlaintextPassword}</p>" +
                    $"<p><em>Vui lòng đổi mật khẩu ngay sau khi đăng nhập lần đầu.</em></p>");
            }
            else if (provisionResult.Kind == "Upgraded")
            {
                emailSent = await _emailService.SendEmailAsync(
                    dto.Email,
                    "[HRMS] Tài khoản đã được nâng cấp lên Nhân viên",
                    $"<h2>Chúc mừng!</h2>" +
                    $"<p>Tài khoản của bạn ({dto.Email}) đã được nâng cấp từ Ứng viên lên Nhân viên.</p>" +
                    $"<p>Bạn vẫn sử dụng mật khẩu hiện tại để đăng nhập.</p>");
            }
            // Kind == "Linked": không gửi email (tài khoản nội bộ đã tồn tại)

            if (!emailSent)
            {
                _logger.LogWarning(
                    "Failed to send onboarding email to {Email} for Employee {EmployeeId}. " +
                    "Admin cần thông báo thủ công.",
                    dto.Email, employee.EmployeeId);
            }

            // Load Department cho projection
            await _context.Entry(employee).Reference(e => e.Department).LoadAsync();

            _eventLogger.LogEvent("Employee",
                $"created (Id: {employee.EmployeeId}, Name: {employee.FullName}, Provision: {provisionResult.Kind})",
                _eventLogger.OnEntityCreated);

            return new EmployeeCreateResponseDto
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Email = employee.Email,
                Position = employee.Position,
                JoinDate = employee.JoinDate,
                Salary = employee.Salary,
                Status = employee.Status.ToString(),
                DepartmentName = employee.Department?.Name ?? "N/A",
                CreatedAt = employee.CreatedAt,
                // Password CHỈ trả về khi NewAccount, không log, không lưu lại
                InitialPassword = provisionResult.Kind == "NewAccount"
                    ? provisionResult.PlaintextPassword
                    : null,
                EmailSent = emailSent
            };
        }

        public async Task<bool> UpdateAsync(int employeeId, EmployeeUpdateDto dto)
        {
            var employee = await _context.Employees.FindAsync(employeeId)
                ?? throw new NotFoundException("Employee", employeeId);

            // Kiểm tra DepartmentId tồn tại (ràng buộc FK)
            var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
            if (!departmentExists)
                throw new BusinessRuleException($"Department with ID {dto.DepartmentId} does not exist.");

            // Anemic model: set properties trực tiếp
            employee.FullName = dto.FullName;
            employee.Email = dto.Email;
            employee.Position = dto.Position;
            employee.JoinDate = dto.JoinDate;
            employee.DepartmentId = dto.DepartmentId;
            employee.Salary = dto.Salary;

            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Employee", $"updated (Id: {employeeId})", _eventLogger.OnEntityUpdated);
            return true;
        }

        public async Task<bool> ChangeStatusAsync(int employeeId, EmployeeStatus status)
        {
            var employee = await _context.Employees.FindAsync(employeeId)
                ?? throw new NotFoundException("Employee", employeeId);

            employee.ChangeStatus(status);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Employee", $"status changed to {status} (Id: {employeeId})",
                _eventLogger.OnEntityUpdated);
            return true;
        }

        public async Task<int?> GetLinkedUserIdAsync(int employeeId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
            return user?.Id;
        }

        public async Task<bool> DeleteAsync(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId)
                ?? throw new NotFoundException("Employee", employeeId);

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Employee", $"deleted (Id: {employeeId})", _eventLogger.OnEntityDeleted);
            return true;
        }

        public async Task<int> DeleteAllAsync()
        {
            var count = await _context.Employees.ExecuteDeleteAsync();

            _eventLogger.LogEvent("Employee", $"all deleted ({count} records)", _eventLogger.OnEntityDeleted);
            return count;
        }
    }
}
