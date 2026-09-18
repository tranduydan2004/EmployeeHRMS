using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Extensions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace EmployeeHRMS.Tests.Services
{
    public class ApplicationServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ApplicationService _service;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly EventLogger _eventLogger;

        public ApplicationServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            var mockLogger = new Mock<ILogger<EventLogger>>();
            _eventLogger = new EventLogger(mockLogger.Object);
            _httpContextAccessor = new Mock<IHttpContextAccessor>();

            // Default: Admin user (sees all)
            SetupUserContext("Admin", 1);

            _service = new ApplicationService(_context, _eventLogger, _httpContextAccessor.Object);

            SeedData();
        }

        private void SetupUserContext(string role, int userId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        }

        private void SeedData()
        {
            var dept = new Department { Id = 1, Name = "Engineering" };
            _context.Departments.Add(dept);

            var candidate1 = new Candidate
            {
                Id = 1, FullName = "Candidate A", Email = "a@test.com",
                Phone = "111", ResumeUrl = "", UserId = 10
            };
            var candidate2 = new Candidate
            {
                Id = 2, FullName = "Candidate B", Email = "b@test.com",
                Phone = "222", ResumeUrl = "", UserId = 20
            };
            _context.Candidates.AddRange(candidate1, candidate2);

            var job = new JobPosting
            {
                Id = 1, Title = "Backend Dev", Description = "C# dev",
                DepartmentId = 1, Status = JobPostingStatus.Published
            };
            _context.JobPostings.Add(job);

            // Application from Candidate A
            var app1 = new Application
            {
                Id = 1, CandidateId = 1, JobPostingId = 1,
                AppliedDate = DateTime.UtcNow
            };
            app1.SetInitialStatus();
            _context.Applications.Add(app1);

            _context.SaveChanges();
        }

        [Fact]
        public async Task CreateAsync_DuplicateApplication_ThrowsBusinessRuleException()
        {
            // Arrange — same CandidateId + JobPostingId as existing application
            var dto = new ApplicationCreateDto
            {
                CandidateId = 1,
                JobPostingId = 1
            };

            // Act & Assert
            var act = () => _service.CreateAsync(dto);
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*ứng tuyển*");
        }

        [Fact]
        public async Task UpdateStatusAsync_ValidTransition_UpdatesStatusSuccessfully()
        {
            // Act — Applied → Screening (valid transition)
            var result = await _service.UpdateStatusAsync(1, ApplicationStatus.Screening);

            // Assert
            result.Should().BeTrue();
            var app = await _context.Applications.FindAsync(1);
            app!.Status.Should().Be(ApplicationStatus.Screening);
        }

        [Fact]
        public async Task UpdateStatusAsync_InvalidTransition_ThrowsBusinessRuleException()
        {
            // Arrange — first transition to Screening
            await _service.UpdateStatusAsync(1, ApplicationStatus.Screening);

            // Act — Screening → Applied (invalid transition)
            var act = () => _service.UpdateStatusAsync(1, ApplicationStatus.Applied);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Invalid status transition*");
        }

        [Fact]
        public async Task GetAllAsync_AsCandidateRole_OnlyReturnsOwnApplications()
        {
            // Arrange — switch to Candidate role (UserId = 10 = Candidate A's user)
            SetupUserContext("Candidate", 10);
            var service = new ApplicationService(_context, _eventLogger, _httpContextAccessor.Object);

            // Also add application for Candidate B to ensure filtering works
            var app2 = new Application
            {
                Id = 2, CandidateId = 2, JobPostingId = 1,
                AppliedDate = DateTime.UtcNow
            };
            app2.SetInitialStatus();
            _context.Applications.Add(app2);
            await _context.SaveChangesAsync();

            var paginationParams = new PaginationParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await service.GetAllAsync(paginationParams);

            // Assert — should only see Candidate A's application, not B's
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1); // TotalCount also filtered
            result.Items[0].CandidateName.Should().Be("Candidate A");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
