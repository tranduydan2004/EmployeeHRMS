using System.Security.Claims;
using EmployeeHRMS.Api.Authorization;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Tests.Services
{
    public class InterviewAuthorizationHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InterviewAuthorizationHandler _handler;

        public InterviewAuthorizationHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _handler = new InterviewAuthorizationHandler(_context);

            SeedData();
        }

        private void SeedData()
        {
            var dept = new Department { Id = 1, Name = "Engineering" };
            var candidate = new Candidate
            {
                Id = 1,
                FullName = "Candidate 1",
                Email = "candidate1@example.com",
                UserId = 100
            };
            var job = new JobPosting
            {
                Id = 1,
                Title = "Backend Dev",
                DepartmentId = 1,
                Status = JobPostingStatus.Published
            };
            var app = new Application
            {
                Id = 1,
                CandidateId = 1,
                JobPostingId = 1
            };
            app.SetInitialStatus();

            var interview = new Interview
            {
                Id = 1,
                ApplicationId = 1,
                InterviewerId = 50, // Assigned interviewer UserId
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();

            _context.Departments.Add(dept);
            _context.Candidates.Add(candidate);
            _context.JobPostings.Add(job);
            _context.Applications.Add(app);
            _context.Interviews.Add(interview);
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
        public async Task Admin_CanEvaluateAndRead_Succeeds()
        {
            var interview = await _context.Interviews.FirstAsync(i => i.Id == 1);
            var user = CreateUserPrincipal(1, "Admin");

            var evalContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Evaluate }, user, interview);
            await _handler.HandleAsync(evalContext);
            evalContext.HasSucceeded.Should().BeTrue();

            var readContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Read }, user, interview);
            await _handler.HandleAsync(readContext);
            readContext.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HR_CanEvaluateAndRead_Succeeds()
        {
            var interview = await _context.Interviews.FirstAsync(i => i.Id == 1);
            var user = CreateUserPrincipal(2, "HR");

            var evalContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Evaluate }, user, interview);
            await _handler.HandleAsync(evalContext);
            evalContext.HasSucceeded.Should().BeTrue();

            var readContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Read }, user, interview);
            await _handler.HandleAsync(readContext);
            readContext.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task AssignedInterviewer_CanEvaluateAndRead_Succeeds()
        {
            var interview = await _context.Interviews.FirstAsync(i => i.Id == 1);
            var user = CreateUserPrincipal(50, "Interviewer"); // InterviewerId = 50

            var evalContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Evaluate }, user, interview);
            await _handler.HandleAsync(evalContext);
            evalContext.HasSucceeded.Should().BeTrue();

            var readContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Read }, user, interview);
            await _handler.HandleAsync(readContext);
            readContext.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task UnassignedInterviewer_CannotEvaluateOrRead_Fails()
        {
            var interview = await _context.Interviews.FirstAsync(i => i.Id == 1);
            var user = CreateUserPrincipal(99, "Interviewer"); // Not 50

            var evalContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Evaluate }, user, interview);
            await _handler.HandleAsync(evalContext);
            evalContext.HasSucceeded.Should().BeFalse();

            var readContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Read }, user, interview);
            await _handler.HandleAsync(readContext);
            readContext.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task CandidateOwner_CanRead_CannotEvaluate()
        {
            var interview = await _context.Interviews.FirstAsync(i => i.Id == 1);
            var user = CreateUserPrincipal(100, "Candidate"); // Candidate UserId = 100

            var readContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Read }, user, interview);
            await _handler.HandleAsync(readContext);
            readContext.HasSucceeded.Should().BeTrue();

            var evalContext = new AuthorizationHandlerContext(new[] { ResourceOperations.Evaluate }, user, interview);
            await _handler.HandleAsync(evalContext);
            evalContext.HasSucceeded.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
