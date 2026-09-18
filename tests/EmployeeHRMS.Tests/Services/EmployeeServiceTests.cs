using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmployeeHRMS.Tests.Services
{
    public class EmployeeServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly EmployeeService _service;

        public EmployeeServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            var mockLogger = new Mock<ILogger<EventLogger>>();
            var eventLogger = new EventLogger(mockLogger.Object);
            var mockEmailService = new Mock<IEmailService>();
            var provisioningService = new UserProvisioningService(_context);
            var mockEmpLogger = new Mock<ILogger<EmployeeService>>();
            _service = new EmployeeService(_context, eventLogger, mockEmailService.Object, provisioningService, mockEmpLogger.Object);

            // Seed test data
            SeedData();
        }

        private void SeedData()
        {
            var department = new Department { Id = 1, Name = "Engineering" };
            _context.Departments.Add(department);

            _context.Employees.AddRange(
                new Employee
                {
                    FullName = "Alice Nguyen", Email = "alice@test.com",
                    Position = "Backend Dev", JoinDate = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    Salary = 5000, DepartmentId = 1, Status = EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    FullName = "Bob Tran", Email = "bob@test.com",
                    Position = "Frontend Dev", JoinDate = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    Salary = 4500, DepartmentId = 1, Status = EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    FullName = "Charlie Le", Email = "charlie@test.com",
                    Position = "DevOps", JoinDate = new DateTime(2024, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                    Salary = 6000, DepartmentId = 1, Status = EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedEmployeeAndSavesDb()
        {
            // Arrange
            var dto = new EmployeeCreateDto
            {
                FullName = "Dan Pham",
                Email = "dan@test.com",
                Position = "QA",
                JoinDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                DepartmentId = 1,
                Salary = 3500
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.FullName.Should().Be("Dan Pham");
            result.Email.Should().Be("dan@test.com");
            result.Salary.Should().Be(3500);
            result.DepartmentName.Should().Be("Engineering");

            // Verify saved in DB
            var dbEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == "dan@test.com");
            dbEmployee.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsCorrectDto()
        {
            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.FullName.Should().Be("Alice Nguyen");
            result.DepartmentName.Should().Be("Engineering");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
        {
            // Act & Assert
            var act = () => _service.GetByIdAsync(999);
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*999*");
        }

        [Fact]
        public async Task GetAllAsync_WithPaginationAndSorting_ReturnsPagedResult()
        {
            // Arrange — sort by salary descending, page 1, size 2
            var paginationParams = new PaginationParams
            {
                PageNumber = 1,
                PageSize = 2,
                SortBy = "salary",
                IsDescending = true
            };

            // Act
            var result = await _service.GetAllAsync(paginationParams);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(3);
            result.Items.Should().HaveCount(2);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.TotalPages.Should().Be(2);
            result.HasNextPage.Should().BeTrue();
            result.HasPreviousPage.Should().BeFalse();

            // First item should be highest salary (Charlie: 6000)
            result.Items[0].FullName.Should().Be("Charlie Le");
            result.Items[0].Salary.Should().Be(6000);
        }

        [Fact]
        public async Task GetAllAsync_InvalidSortBy_FallsBackToDefaultSort()
        {
            // Arrange — invalid sort column
            var paginationParams = new PaginationParams
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "invalid_column"
            };

            // Act — should NOT throw, should fallback to default sort (EmployeeId asc)
            var result = await _service.GetAllAsync(paginationParams);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(3);
            result.Items.Should().HaveCount(3);
            result.Items[0].FullName.Should().Be("Alice Nguyen"); // First by EmployeeId
        }

        [Fact]
        public async Task GetByStatusAsync_ReturnsMatchingEmployees()
        {
            // Act
            var result = await _service.GetByStatusAsync(EmployeeStatus.Active);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetMeAsync_UserLinkedToEmployee_ReturnsEmployeeProfile()
        {
            // Arrange
            var user = new ApplicationUser("alice.user@test.com", "hash", UserRole.Employee)
            {
                EmployeeId = 1
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetMeAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.FullName.Should().Be("Alice Nguyen");
            result.Email.Should().Be("alice@test.com");
        }

        [Fact]
        public async Task GetMyDepartmentColleaguesAsync_ReturnsColleaguesExcludingSelf()
        {
            // Arrange
            var user = new ApplicationUser("alice.user@test.com", "hash", UserRole.Employee)
            {
                EmployeeId = 1
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _service.GetMyDepartmentColleaguesAsync(user.Id)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().NotContain(c => c.Id == 1);
            result.Select(c => c.FullName).Should().Contain(new[] { "Bob Tran", "Charlie Le" });
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
