using System.Net;
using System.Security.Claims;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EmployeeHRMS.Tests.Services
{
    public class InterviewServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InterviewService _service;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly EventLogger _eventLogger;
        private readonly AudioInterviewOptions _options;

        public InterviewServiceTests()
        {
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(dbOptions);
            var mockLogger = new Mock<ILogger<EventLogger>>();
            _eventLogger = new EventLogger(mockLogger.Object);
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _emailServiceMock = new Mock<IEmailService>();

            _options = new AudioInterviewOptions
            {
                DefaultThinkSeconds = 30,
                DefaultAnswerSeconds = 90,
                DefaultInviteExpiryDays = 3,
                BaseCandidatePortalUrl = "http://localhost:5173/interview"
            };

            SetupUserContext("Admin", 1);

            _service = new InterviewService(
                _context,
                _eventLogger,
                _httpContextAccessor.Object,
                Options.Create(_options),
                _emailServiceMock.Object);
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

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<Interview> CreateTestInterviewAsync(Action<Interview>? configure = null)
        {
            var dept = new Department { Name = "Engineering" };
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            var job = new JobPosting { Title = "Developer", DepartmentId = dept.Id, Status = JobPostingStatus.Approved };
            _context.JobPostings.Add(job);

            var candidate = new Candidate { FullName = "Candidate", Email = "c@test.com", Phone = "123", ResumeUrl = "" };
            _context.Candidates.Add(candidate);

            var app = new Application { JobPostingId = job.Id, CandidateId = candidate.Id };
            app.SetInitialStatus();
            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            var interview = new Interview { ApplicationId = app.Id };
            interview.Invite(TimeSpan.FromDays(2));
            configure?.Invoke(interview);

            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();
            return interview;
        }

        [Fact]
        public async Task CreateInviteAsync_WhenApplicationNotFound_ShouldThrowNotFoundException()
        {
            // Act
            var act = () => _service.CreateInviteAsync(999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Application*");
        }

        [Fact]
        public async Task CreateInviteAsync_WhenJobPostingDraft_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var dept = new Department { Id = 1, Name = "HR" };
            _context.Departments.Add(dept);

            var job = new JobPosting
            {
                Id = 1,
                Title = "Backend Dev",
                DepartmentId = 1,
                Status = JobPostingStatus.Draft
            };
            _context.JobPostings.Add(job);

            var candidate = new Candidate
            {
                Id = 1,
                FullName = "Nguyen Van A",
                Email = "a@test.com",
                Phone = "0123456789",
                ResumeUrl = "resume.pdf"
            };
            _context.Candidates.Add(candidate);

            var app = new Application
            {
                Id = 10,
                JobPostingId = 1,
                CandidateId = 1
            };
            app.SetInitialStatus();
            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            // Act
            var act = () => _service.CreateInviteAsync(10);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*duyệt*");
        }

        [Fact]
        public async Task CreateInviteAsync_WhenNoQuestionBankItems_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var dept = new Department { Id = 1, Name = "HR" };
            _context.Departments.Add(dept);

            var job = new JobPosting
            {
                Id = 1,
                Title = "Backend Dev",
                DepartmentId = 1,
                Status = JobPostingStatus.Approved
            };
            _context.JobPostings.Add(job);

            var candidate = new Candidate
            {
                Id = 1,
                FullName = "Nguyen Van A",
                Email = "a@test.com",
                Phone = "0123456789",
                ResumeUrl = "resume.pdf"
            };
            _context.Candidates.Add(candidate);

            var app = new Application
            {
                Id = 10,
                JobPostingId = 1,
                CandidateId = 1
            };
            app.SetInitialStatus();
            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            // Act
            var act = () => _service.CreateInviteAsync(10);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*chưa có câu hỏi phỏng vấn*");
        }

        [Fact]
        public async Task CreateInviteAsync_WhenApplicationRejected_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var dept = new Department { Id = 1, Name = "HR" };
            _context.Departments.Add(dept);

            var job = new JobPosting
            {
                Id = 1,
                Title = "Backend Dev",
                DepartmentId = 1,
                Status = JobPostingStatus.Published
            };
            job.QuestionBankItems.Add(new QuestionBankItem
            {
                Id = 1,
                JobPostingId = 1,
                Question = "Tell me about C#",
                OrderIndex = 1
            });
            _context.JobPostings.Add(job);

            var candidate = new Candidate
            {
                Id = 1,
                FullName = "Nguyen Van A",
                Email = "a@test.com",
                Phone = "0123456789",
                ResumeUrl = "resume.pdf"
            };
            _context.Candidates.Add(candidate);

            var app = new Application
            {
                Id = 10,
                JobPostingId = 1,
                CandidateId = 1
            };
            app.SetInitialStatus();
            app.TransitionTo(ApplicationStatus.Rejected);
            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            // Act
            var act = () => _service.CreateInviteAsync(10);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Không thể mời phỏng vấn hồ sơ*");
        }

        [Fact]
        public async Task CreateInviteAsync_WhenValid_ShouldCreateInterviewAndCopyQuestionsWithRubricClones()
        {
            // Arrange
            var dept = new Department { Id = 1, Name = "HR" };
            _context.Departments.Add(dept);

            var job = new JobPosting
            {
                Id = 1,
                Title = ".NET Senior Engineer",
                DepartmentId = 1,
                Status = JobPostingStatus.Approved
            };

            var rubric1 = new ScoringRubric
            {
                Excellent = "Mastery of async/await",
                Good = "Understands tasks",
                Acceptable = "Basic knowledge",
                Poor = "No clue"
            };

            job.QuestionBankItems.Add(new QuestionBankItem
            {
                Id = 101,
                JobPostingId = 1,
                Question = "How does async/await work in .NET?",
                OrderIndex = 1,
                ThinkSeconds = 40,
                AnswerSeconds = 120,
                ScoringRubric = rubric1
            });

            job.QuestionBankItems.Add(new QuestionBankItem
            {
                Id = 102,
                JobPostingId = 1,
                Question = "Explain EF Core tracking vs no-tracking",
                OrderIndex = 2,
                ThinkSeconds = 30,
                AnswerSeconds = 90
            });

            _context.JobPostings.Add(job);

            var candidate = new Candidate
            {
                Id = 1,
                FullName = "Tran Duy Dan",
                Email = "dan@example.com",
                Phone = "0987654321",
                ResumeUrl = "cv.pdf"
            };
            _context.Candidates.Add(candidate);

            var app = new Application
            {
                Id = 20,
                JobPostingId = 1,
                CandidateId = 1
            };
            app.SetInitialStatus();
            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            var inviteDto = new InterviewInviteCreateDto
            {
                ValidityDays = 5,
                SendEmail = true
            };

            // Act
            var result = await _service.CreateInviteAsync(20, inviteDto);

            // Assert
            result.Should().NotBeNull();
            result.ApplicationId.Should().Be(20);
            result.InviteToken.Should().NotBeEmpty();
            result.InviteUrl.Should().Be($"http://localhost:5173/interview/{result.InviteToken}");
            result.TotalQuestions.Should().Be(2);
            result.Status.Should().Be("Scheduled");
            result.CandidateName.Should().Be("Tran Duy Dan");
            result.CandidateEmail.Should().Be("dan@example.com");
            result.JobTitle.Should().Be(".NET Senior Engineer");
            result.InviteExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(4.9));

            // Verify Application state transition
            var updatedApp = await _context.Applications.FindAsync(20);
            updatedApp!.Status.Should().Be(ApplicationStatus.Interviewing);

            // Verify Interview in DB
            var dbInterview = await _context.Interviews
                .Include(i => i.Questions)
                .FirstOrDefaultAsync(i => i.Id == result.InterviewId);

            dbInterview.Should().NotBeNull();
            dbInterview!.Questions.Should().HaveCount(2);

            var q1 = dbInterview.Questions.First(q => q.OrderIndex == 1);
            q1.Question.Should().Be("How does async/await work in .NET?");
            q1.ThinkSeconds.Should().Be(40);
            q1.AnswerSeconds.Should().Be(120);
            q1.ScoringRubric.Should().NotBeNull();
            q1.ScoringRubric!.Excellent.Should().Be("Mastery of async/await");
            // Must be distinct instance (cloned)
            q1.ScoringRubric.Should().NotBeSameAs(rubric1);

            var q2 = dbInterview.Questions.First(q => q.OrderIndex == 2);
            q2.ThinkSeconds.Should().Be(30);
            q2.AnswerSeconds.Should().Be(90);
            q2.ScoringRubric.Should().BeNull();

            // Verify email sent
            _emailServiceMock.Verify(m => m.SendEmailAsync(
                "dan@example.com",
                It.Is<string>(s => s.Contains(".NET Senior Engineer")),
                It.Is<string>(b => b.Contains(result.InviteUrl))),
                Times.Once);
        }

        [Fact]
        public async Task GetCandidateSessionAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            // Act
            var act = () => _service.GetCandidateSessionAsync(Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Không tìm thấy phiên phỏng vấn*");
        }

        [Fact]
        public async Task GetCandidateSessionAsync_WhenCancelled_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.Cancel());

            // Act
            var act = () => _service.GetCandidateSessionAsync(interview.InviteToken);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.Which.Message.Should().Contain("hủy");
        }

        [Fact]
        public async Task GetCandidateSessionAsync_WhenCompleted_ShouldThrowConflictException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.Complete());

            // Act
            var act = () => _service.GetCandidateSessionAsync(interview.InviteToken);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*đã hoàn thành*");
        }

        [Fact]
        public async Task GetCandidateSessionAsync_WhenExpired_ShouldMarkExpiredAndThrowGoneException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkExpired());

            // Act
            var act = () => _service.GetCandidateSessionAsync(interview.InviteToken);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Gone);
        }

        [Fact]
        public async Task GetCandidateSessionAsync_WhenValid_ShouldReturnSafePublicFieldsOnly()
        {
            // Arrange
            var dept = new Department { Id = 1, Name = "Engineering" };
            _context.Departments.Add(dept);

            var job = new JobPosting { Id = 1, Title = "Frontend Lead", DepartmentId = 1, Status = JobPostingStatus.Approved };
            _context.JobPostings.Add(job);

            var candidate = new Candidate { Id = 1, FullName = "Candidate One", Email = "c1@test.com", Phone = "123", ResumeUrl = "" };
            _context.Candidates.Add(candidate);

            var app = new Application { Id = 1, JobPostingId = 1, CandidateId = 1 };
            app.SetInitialStatus();
            app.TransitionTo(ApplicationStatus.Screening);
            app.TransitionTo(ApplicationStatus.Interviewing);
            _context.Applications.Add(app);

            var interview = new Interview { Id = 1, ApplicationId = 1 };
            interview.Invite(TimeSpan.FromDays(3));

            var q1 = new InterviewQuestion
            {
                Id = 11,
                InterviewId = 1,
                Question = "React Server Components vs SSR?",
                OrderIndex = 1,
                ThinkSeconds = 45,
                AnswerSeconds = 120,
                ScoringRubric = new ScoringRubric
                {
                    Excellent = "Super secret internal rubric criteria",
                    Good = "Secret good rubric",
                    Acceptable = "Secret acceptable rubric",
                    Poor = "Secret poor rubric"
                }
            };
            var q2 = new InterviewQuestion
            {
                Id = 12,
                InterviewId = 1,
                Question = "What is hydration?",
                OrderIndex = 2,
                ThinkSeconds = 30,
                AnswerSeconds = 90
            };
            interview.Questions.Add(q1);
            interview.Questions.Add(q2);

            interview.Answers.Add(new InterviewAnswer
            {
                Id = 100,
                InterviewId = 1,
                InterviewQuestionId = 11,
                AudioStorageKey = "audio/q11.webm",
                AudioMimeType = "audio/webm"
            });

            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();

            // Act
            var session = await _service.GetCandidateSessionAsync(interview.InviteToken);

            // Assert
            session.Should().NotBeNull();
            session.InviteToken.Should().Be(interview.InviteToken);
            session.JobTitle.Should().Be("Frontend Lead");
            session.CandidateName.Should().Be("Candidate One");
            session.Status.Should().Be("Scheduled");
            session.TotalQuestions.Should().Be(2);
            session.Questions.Should().HaveCount(2);

            var resQ1 = session.Questions.First(q => q.Id == 11);
            resQ1.Question.Should().Be("React Server Components vs SSR?");
            resQ1.ThinkSeconds.Should().Be(45);
            resQ1.AnswerSeconds.Should().Be(120);
            resQ1.HasAnswer.Should().BeTrue();

            var resQ2 = session.Questions.First(q => q.Id == 12);
            resQ2.Question.Should().Be("What is hydration?");
            resQ2.ThinkSeconds.Should().Be(30);
            resQ2.AnswerSeconds.Should().Be(90);
            resQ2.HasAnswer.Should().BeFalse();

            // CandidateQuestionDto type itself must NOT expose ScoringRubric
            typeof(CandidateQuestionDto).GetProperties().Select(p => p.Name)
                .Should().NotContain("ScoringRubric");
        }

        [Fact]
        public async Task StartCandidateInterviewAsync_WhenValid_ShouldTransitionToInProgressAndSetConsentedAt()
        {
            // Arrange
            var interview = new Interview { Id = 1 };
            interview.Invite(TimeSpan.FromDays(2));
            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.StartCandidateInterviewAsync(interview.InviteToken);

            // Assert
            result.Should().NotBeNull();
            result.InviteToken.Should().Be(interview.InviteToken);
            result.Status.Should().Be("InProgress");
            result.ConsentedAt.Should().NotBeNull();

            var dbInterview = await _context.Interviews.FindAsync(1);
            dbInterview!.Status.Should().Be(InterviewStatus.InProgress);
            dbInterview.ConsentedAt.Should().Be(result.ConsentedAt);
        }

        [Fact]
        public async Task StartCandidateInterviewAsync_WhenAlreadyInProgress_ShouldBeIdempotentAndPreserveConsentedAt()
        {
            // Arrange
            var interview = new Interview { Id = 1 };
            interview.Invite(TimeSpan.FromDays(2));
            interview.MarkInProgress();
            var initialConsentedAt = interview.ConsentedAt;
            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.StartCandidateInterviewAsync(interview.InviteToken);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("InProgress");
            result.ConsentedAt.Should().Be(initialConsentedAt);
        }

        [Fact]
        public async Task StartCandidateInterviewAsync_WhenExpired_ShouldThrowGoneException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkExpired());

            // Act
            var act = () => _service.StartCandidateInterviewAsync(interview.InviteToken);

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Gone);
        }

        [Fact]
        public async Task StartCandidateInterviewAsync_WhenCompleted_ShouldThrowConflictException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.Complete());

            // Act
            var act = () => _service.StartCandidateInterviewAsync(interview.InviteToken);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*đã hoàn thành*");
        }
    }
}
