using EmployeeHRMS.Api.Authorization;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeHRMS.Tests.Services
{
    public class EmployeeAuthorizationHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly EmployeeAuthorizationHandler _handler;

        public EmployeeAuthorizationHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _handler = new EmployeeAuthorizationHandler(_context);

            SeedData();
        }

        private void SeedData()
        {
            var user1 = new ApplicationUser("emp1@test.com", "hash", UserRole.Employee)
            {
                EmployeeId = 1
            };
            var user2 = new ApplicationUser("emp2@test.com", "hash", UserRole.Employee)
            {
                EmployeeId = 2
            };

            _context.Users.AddRange(user1, user2);
            _context.SaveChanges();
        }

        private static ClaimsPrincipal CreateUserPrincipal(int userId, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        [Fact]
        public async Task Admin_CanPerformAnyOperation_Succeeds()
        {
            // Arrange
            var user = CreateUserPrincipal(999, nameof(UserRole.Admin));
            var employee = new Employee { EmployeeId = 1 };
            var authContext = new AuthorizationHandlerContext(
                new[] { ResourceOperations.Read },
                user,
                employee);

            // Act
            await _handler.HandleAsync(authContext);

            // Assert
            authContext.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HR_CanPerformAnyOperation_Succeeds()
        {
            // Arrange
            var user = CreateUserPrincipal(998, nameof(UserRole.HR));
            var employee = new Employee { EmployeeId = 1 };
            var authContext = new AuthorizationHandlerContext(
                new[] { ResourceOperations.Read },
                user,
                employee);

            // Act
            await _handler.HandleAsync(authContext);

            // Assert
            authContext.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Employee_ReadOwnProfile_Succeeds()
        {
            // Arrange — User 1 has EmployeeId = 1
            var user = CreateUserPrincipal(1, nameof(UserRole.Employee));
            var employee = new Employee { EmployeeId = 1 };
            var authContext = new AuthorizationHandlerContext(
                new[] { ResourceOperations.Read },
                user,
                employee);

            // Act
            await _handler.HandleAsync(authContext);

            // Assert
            authContext.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Employee_ReadOtherEmployeeProfile_Fails()
        {
            // Arrange — User 1 tries to read Employee 2's profile
            var user = CreateUserPrincipal(1, nameof(UserRole.Employee));
            var employee = new Employee { EmployeeId = 2 };
            var authContext = new AuthorizationHandlerContext(
                new[] { ResourceOperations.Read },
                user,
                employee);

            // Act
            await _handler.HandleAsync(authContext);

            // Assert
            authContext.HasSucceeded.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
