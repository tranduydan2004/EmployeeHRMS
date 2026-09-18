using EmployeeHRMS.Api.Authorization;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeHRMS.Tests.Services
{
    public class CandidateAuthorizationHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly CandidateAuthorizationHandler _handler;

        public CandidateAuthorizationHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _handler = new CandidateAuthorizationHandler(_context);

            SeedData();
        }

        private void SeedData()
        {
            // Candidate A's user account (UserId = 10)
            _context.Candidates.Add(new Candidate
            {
                Id = 1, FullName = "Candidate A", Email = "a@test.com",
                Phone = "111", ResumeUrl = "uploads/resumes/a.pdf", UserId = 10
            });

            // Candidate B's profile (UserId = 20)
            _context.Candidates.Add(new Candidate
            {
                Id = 2, FullName = "Candidate B", Email = "b@test.com",
                Phone = "222", ResumeUrl = "uploads/resumes/b.pdf", UserId = 20
            });

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
        public async Task CandidateA_CannotReadResume_OfCandidateB_Fails()
        {
            // Arrange — Candidate A (UserId=10) tries to Read Candidate B's profile
            var candidateB = await _context.Candidates
                .Include(c => c.Applications)
                .FirstAsync(c => c.Id == 2);

            var user = CreateUserPrincipal(10, "Candidate");
            var requirement = ResourceOperations.Read;

            var authContext = new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                candidateB);

            // Act
            await _handler.HandleAsync(authContext);

            // Assert — should NOT succeed (Candidate A ≠ UserId 20)
            authContext.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task CandidateA_CanReadOwn_Profile_Succeeds()
        {
            // Arrange — Candidate A (UserId=10) reads own profile
            var candidateA = await _context.Candidates
                .Include(c => c.Applications)
                .FirstAsync(c => c.Id == 1);

            var user = CreateUserPrincipal(10, "Candidate");
            var requirement = ResourceOperations.Read;

            var authContext = new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                candidateA);

            // Act
            await _handler.HandleAsync(authContext);

            // Assert
            authContext.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Admin_CanReadAnyProfile_Succeeds()
        {
            // Arrange — Admin reads Candidate B's profile
            var candidateB = await _context.Candidates
                .Include(c => c.Applications)
                .FirstAsync(c => c.Id == 2);

            var user = CreateUserPrincipal(1, "Admin");
            var requirement = ResourceOperations.Read;

            var authContext = new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                candidateB);

            // Act
            await _handler.HandleAsync(authContext);

            // Assert
            authContext.HasSucceeded.Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
