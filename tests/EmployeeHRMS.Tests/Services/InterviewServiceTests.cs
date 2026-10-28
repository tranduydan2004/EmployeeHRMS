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
        private readonly Mock<IAudioTranscriptionQueue> _queueMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IAudioStorageService> _audioStorageServiceMock;
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
            _queueMock = new Mock<IAudioTranscriptionQueue>();
            _notificationServiceMock = new Mock<INotificationService>();
            _audioStorageServiceMock = new Mock<IAudioStorageService>();

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
                _emailServiceMock.Object,
                _queueMock.Object,
                audioStorageService: _audioStorageServiceMock.Object,
                notificationService: _notificationServiceMock.Object);
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
                Category = QuestionCategory.Technical,
                OrderIndex = 2,
                ThinkSeconds = 30,
                AnswerSeconds = 90,
                ScoringRubric = new ScoringRubric
                {
                    Excellent = "Detailed knowledge of change tracker",
                    Good = "Understands AsNoTracking benefit",
                    Acceptable = "Basic knowledge",
                    Poor = "No understanding"
                }
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

            // Verify Application state transition: audio screening stays in Screening stage
            var updatedApp = await _context.Applications.FindAsync(20);
            updatedApp!.Status.Should().Be(ApplicationStatus.Screening);

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
            q2.Category.Should().Be(QuestionCategory.Technical);
            q2.ScoringRubric.Should().NotBeNull();

            // Verify email sent
            _emailServiceMock.Verify(m => m.SendEmailAsync(
                "dan@example.com",
                It.Is<string>(s => s.Contains(".NET Senior Engineer")),
                It.Is<string>(b => b.Contains(result.InviteUrl))),
                Times.Once);
        }

        [Fact]
        public async Task CreateInviteAsync_WhenQuestionMissingRubric_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var dept = new Department { Name = "HR" };
            _context.Departments.Add(dept);

            var job = new JobPosting
            {
                Title = "Fullstack Engineer",
                Department = dept,
                Status = JobPostingStatus.Approved
            };

            // Câu hỏi 1 có rubric
            job.QuestionBankItems.Add(new QuestionBankItem
            {
                Question = "Tell us about React",
                OrderIndex = 1,
                ScoringRubric = new ScoringRubric
                {
                    Excellent = "E",
                    Good = "G",
                    Acceptable = "A",
                    Poor = "P"
                }
            });

            // Câu hỏi 2 thiếu rubric
            job.QuestionBankItems.Add(new QuestionBankItem
            {
                Question = "Tell us about Docker",
                OrderIndex = 2,
                ScoringRubric = null
            });

            _context.JobPostings.Add(job);

            var candidate = new Candidate { FullName = "Bob", Email = "bob@example.com", Phone = "123", ResumeUrl = "" };
            _context.Candidates.Add(candidate);

            var app = new Application { JobPosting = job, Candidate = candidate };
            app.SetInitialStatus();
            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            // Act
            var act = () => _service.CreateInviteAsync(app.Id);

            // Assert (D3)
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*chưa có đầy đủ Scoring Rubric 4 mức*");
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
            var interview = await CreateTestInterviewAsync(i => { i.MarkInProgress(); i.Complete(); });

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
                ThinkSeconds = 40,
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
            resQ1.ThinkSeconds.Should().Be(40);
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
            var interview = await CreateTestInterviewAsync(i => { i.MarkInProgress(); i.Complete(); });

            // Act
            var act = () => _service.StartCandidateInterviewAsync(interview.InviteToken);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*đã hoàn thành*");
        }

        [Fact]
        public async Task ResendInviteAsync_WhenInterviewNotFound_ShouldThrowNotFoundException()
        {
            var act = () => _service.ResendInviteAsync(99999);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ResendInviteAsync_WhenNotAudioInterview_ShouldThrowBusinessRuleException()
        {
            var dept = new Department { Name = "Dept" };
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            var job = new JobPosting { Title = "Job", DepartmentId = dept.Id, Status = JobPostingStatus.Approved };
            _context.JobPostings.Add(job);

            var candidate = new Candidate { FullName = "Cand", Email = "c@test.com", Phone = "123", ResumeUrl = "" };
            _context.Candidates.Add(candidate);

            var app = new Application { JobPostingId = job.Id, CandidateId = candidate.Id };
            app.SetInitialStatus();
            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            var directInterview = new Interview { ApplicationId = app.Id, ScheduledDate = DateTime.UtcNow.AddDays(1) };
            directInterview.Schedule();
            _context.Interviews.Add(directInterview);
            await _context.SaveChangesAsync();

            var act = () => _service.ResendInviteAsync(directInterview.Id);
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*không phải là phỏng vấn trực tuyến qua liên kết*");
        }

        [Fact]
        public async Task ResendInviteAsync_WhenValid_ShouldSendEmailAndReturnDto()
        {
            _emailServiceMock
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var interview = await CreateTestInterviewAsync();

            var result = await _service.ResendInviteAsync(interview.Id);

            result.Should().NotBeNull();
            result.InterviewId.Should().Be(interview.Id);
            result.InviteToken.Should().Be(interview.InviteToken);
            result.InviteUrl.Should().Contain(interview.InviteToken.ToString());
            result.EmailSent.Should().BeTrue();
        }

        // ============================================================
        // AI Rubric Grading & Re-evaluation Tests (Phase 4 Step 6)
        // ============================================================

        [Fact]
        public async Task UpdateTranscriptAsync_WhenValid_UpdatesEditedTranscriptAndLastEditedMetadata()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var question = new InterviewQuestion
            {
                InterviewId = interview.Id,
                Question = "Tell me about your experience",
                OrderIndex = 1
            };
            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestion = question,
                AudioStorageKey = "audio/test.webm",
                AudioDurationSeconds = 45
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Transcript goc tu Whisper");
            answer.CompleteScoring();

            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateTranscriptAsync(
                interview.Id, question.Id, "Bản bóc băng đã được HR hiệu đính", 42);

            // Assert
            result.Should().NotBeNull();
            result.Transcript.Should().Be("Transcript goc tu Whisper");
            result.EditedTranscript.Should().Be("Bản bóc băng đã được HR hiệu đính");
            result.EffectiveTranscript.Should().Be("Bản bóc băng đã được HR hiệu đính");
            result.LastEditedByUserId.Should().Be(42);
            result.LastEditedAt.Should().NotBeNull();

            var dbAnswer = await _context.InterviewAnswers.FindAsync(answer.Id);
            dbAnswer!.EditedTranscript.Should().Be("Bản bóc băng đã được HR hiệu đính");
            dbAnswer.LastEditedByUserId.Should().Be(42);
        }

        [Fact]
        public async Task UpdateTranscriptAsync_WhenTranscriptIsEmpty_ThrowsBusinessRuleException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var question = new InterviewQuestion
            {
                InterviewId = interview.Id,
                Question = "Tell me about your experience",
                OrderIndex = 1
            };
            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestion = question,
                AudioStorageKey = "audio/test.webm"
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Goc");
            answer.CompleteScoring();

            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act & Assert
            var act = () => _service.UpdateTranscriptAsync(interview.Id, question.Id, "   ", 42);
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*không được để trống*");
        }

        [Fact]
        public async Task UpdateTranscriptAsync_WhenAnswerIsTranscribing_ThrowsBusinessRuleException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var question = new InterviewQuestion
            {
                InterviewId = interview.Id,
                Question = "Q",
                OrderIndex = 1
            };
            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestion = question,
                AudioStorageKey = "audio/test.webm"
            };
            answer.StartTranscribing();

            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act & Assert
            var act = () => _service.UpdateTranscriptAsync(interview.Id, question.Id, "Edited text", 42);
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*đang trong quá trình bóc băng*");
        }

        [Fact]
        public async Task UpdateTranscriptAsync_WhenAnswerIsCurrentlyScoring_ThrowsConflictException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var question = new InterviewQuestion
            {
                InterviewId = interview.Id,
                Question = "Q",
                OrderIndex = 1
            };
            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestion = question,
                AudioStorageKey = "audio/test.webm"
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Goc");
            // Status is now Scoring

            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act & Assert
            var act = () => _service.UpdateTranscriptAsync(interview.Id, question.Id, "Edited text", 42);
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*Đang trong tiến trình chấm điểm*");
        }

        [Fact]
        public async Task ReevaluateAnswerAsync_WhenValidCompletedAnswer_TransitionsToScoringAndEnqueuesJob()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var question = new InterviewQuestion
            {
                InterviewId = interview.Id,
                Question = "Tell me about SOLID",
                OrderIndex = 1
            };
            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestion = question,
                AudioStorageKey = "audio/solid.webm"
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Ban transcript hop le");
            answer.CompleteScoring();

            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ReevaluateAnswerAsync(interview.Id, question.Id, 99);

            // Assert
            result.Should().NotBeNull();
            result.InterviewAnswerId.Should().Be(answer.Id);
            result.QuestionId.Should().Be(question.Id);
            result.Status.Should().Be("Scoring");
            result.Message.Should().Contain("hàng đợi xử lý");

            var dbAnswer = await _context.InterviewAnswers.FindAsync(answer.Id);
            dbAnswer!.Status.Should().Be(AudioProcessingStatus.Scoring);

            _queueMock.Verify(q => q.EnqueueAsync(
                It.Is<AudioTranscriptionJob>(j => j.InterviewAnswerId == answer.Id),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReevaluateAnswerAsync_WhenAnswerIsCurrentlyScoring_ThrowsConflictException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var question = new InterviewQuestion
            {
                InterviewId = interview.Id,
                Question = "Tell me about SOLID",
                OrderIndex = 1
            };
            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestion = question,
                AudioStorageKey = "audio/solid.webm"
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Ban transcript hop le");
            // Status is now Scoring

            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act & Assert (Q3: Tránh hai lần chấm lại chạy chồng lấn)
            var act = () => _service.ReevaluateAnswerAsync(interview.Id, question.Id, 99);
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*không thể re-evaluate chồng chéo*");
        }

        [Fact]
        public async Task ReevaluateAnswerAsync_WhenAnswerLacksTranscript_ThrowsBusinessRuleException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var question = new InterviewQuestion
            {
                InterviewId = interview.Id,
                Question = "Tell me about SOLID",
                OrderIndex = 1
            };
            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestion = question,
                AudioStorageKey = "audio/solid.webm"
            };
            answer.MarkFailed("Whisper STT failed");

            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act & Assert
            var act = () => _service.ReevaluateAnswerAsync(interview.Id, question.Id, 99);
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*chưa có nội dung bóc băng*");
        }

        [Fact]
        public async Task GetInterviewEvaluationsAsync_ReturnsEvaluationsOrderedByQuestionAndVersionDescending()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var q1 = new InterviewQuestion { InterviewId = interview.Id, Question = "Q1", OrderIndex = 1 };
            var q2 = new InterviewQuestion { InterviewId = interview.Id, Question = "Q2", OrderIndex = 2 };

            var a1 = new InterviewAnswer { InterviewId = interview.Id, InterviewQuestion = q1, AudioStorageKey = "a1.webm" };
            var a2 = new InterviewAnswer { InterviewId = interview.Id, InterviewQuestion = q2, AudioStorageKey = "a2.webm" };

            _context.InterviewQuestions.AddRange(q1, q2);
            _context.InterviewAnswers.AddRange(a1, a2);
            await _context.SaveChangesAsync();

            var eval1V1 = QuestionEvaluation.CreateSucceeded(a1.Id, 1, "Snap1", RubricLevel.Acceptable, 5.0m, "Eval1 V1", null, null, null, "gpt-4o-mini", "v1.0");
            eval1V1.Deactivate();
            var eval1V2 = QuestionEvaluation.CreateSucceeded(a1.Id, 2, "Snap1-edited", RubricLevel.Good, 7.5m, "Eval1 V2", null, null, null, "gpt-4o-mini", "v1.0");
            var eval2V1 = QuestionEvaluation.CreateSucceeded(a2.Id, 1, "Snap2", RubricLevel.Excellent, 10.0m, "Eval2 V1", null, null, null, "gpt-4o-mini", "v1.0");

            _context.QuestionEvaluations.AddRange(eval1V1, eval1V2, eval2V1);
            await _context.SaveChangesAsync();

            // Act
            var results = await _service.GetInterviewEvaluationsAsync(interview.Id);

            // Assert
            results.Should().HaveCount(3);
            // Q1 should come before Q2, and Version 2 should come before Version 1
            results[0].InterviewAnswerId.Should().Be(a1.Id);
            results[0].VersionNumber.Should().Be(2);
            results[0].IsActive.Should().BeTrue();

            results[1].InterviewAnswerId.Should().Be(a1.Id);
            results[1].VersionNumber.Should().Be(1);
            results[1].IsActive.Should().BeFalse();

            results[2].InterviewAnswerId.Should().Be(a2.Id);
            results[2].VersionNumber.Should().Be(1);
            results[2].IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetQuestionEvaluationsAsync_ReturnsEvaluationsForSpecificQuestion()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var q1 = new InterviewQuestion { InterviewId = interview.Id, Question = "Q1", OrderIndex = 1 };
            var a1 = new InterviewAnswer { InterviewId = interview.Id, InterviewQuestion = q1, AudioStorageKey = "a1.webm" };

            _context.InterviewQuestions.Add(q1);
            _context.InterviewAnswers.Add(a1);
            await _context.SaveChangesAsync();

            var eval1 = QuestionEvaluation.CreateSucceeded(a1.Id, 1, "Snap1", RubricLevel.Good, 7.5m, "Eval1", null, null, null, "gpt-4o-mini", "v1.0");
            _context.QuestionEvaluations.Add(eval1);
            await _context.SaveChangesAsync();

            // Act
            var results = await _service.GetQuestionEvaluationsAsync(interview.Id, q1.Id);

            // Assert
            results.Should().ContainSingle();
            results[0].InterviewAnswerId.Should().Be(a1.Id);
            results[0].Score.Should().Be(7.5m);
        }

        [Fact]
        public async Task UpdateTranscriptAsync_WhenExpectedVersionMismatches_ShouldThrowConflictException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var q = new InterviewQuestion { InterviewId = interview.Id, Question = "Q1", OrderIndex = 1 };
            var a = new InterviewAnswer { InterviewId = interview.Id, InterviewQuestion = q, AudioStorageKey = "a.webm" };
            a.StartTranscribing();
            a.CompleteTranscription("Original transcript"); // Status = Scoring
            // Transition to Completed so HR can edit
            a.CompleteScoring(8.0m, "Feedback");

            _context.InterviewQuestions.Add(q);
            _context.InterviewAnswers.Add(a);
            await _context.SaveChangesAsync();

            // First edit advances EditVersion to 2
            await _service.UpdateTranscriptAsync(interview.Id, q.Id, "Edited once", 10, expectedVersion: 1);

            // Act — another user tries to edit expecting v1 (when current is v2)
            var act = () => _service.UpdateTranscriptAsync(interview.Id, q.Id, "Edited twice with stale version", 11, expectedVersion: 1);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*Bản bóc băng đã được cập nhật bởi người khác*");
        }

        [Fact]
        public async Task AssignInterviewerAsync_WhenValid_ShouldAssignInterviewer()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i => i.MarkInProgress());
            var interviewerUser = new ApplicationUser("interviewer@hrms.com", "hash", UserRole.Interviewer);
            _context.Users.Add(interviewerUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.AssignInterviewerAsync(interview.Id, interviewerUser.Id);

            // Assert
            result.InterviewerId.Should().Be(interviewerUser.Id);
            var dbInterview = await _context.Interviews.FindAsync(interview.Id);
            dbInterview!.InterviewerId.Should().Be(interviewerUser.Id);
        }

        [Fact]
        public async Task GetDashboardSummaryAsync_Admin_ReturnsProjectedSummaries()
        {
            // Arrange
            SetupUserContext("Admin", 1);
            var interview1 = await CreateTestInterviewAsync(i =>
            {
                i.MarkInProgress();
                i.SetAggregationResult(8.5m, InterviewRecommendation.Pass, "Good");
                i.Complete();
            });
            var interview2 = await CreateTestInterviewAsync(i =>
            {
                i.SetAggregationResult(null, null, null);
            });

            // Act
            var result = await _service.GetDashboardSummaryAsync(new InterviewDashboardFilterParams
            {
                PageNumber = 1,
                PageSize = 10
            });

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items.Any(x => x.Id == interview1.Id && x.Recommendation == "Pass" && x.AiOverallScore == 8.5m).Should().BeTrue();
            result.Items.Any(x => x.Id == interview2.Id && x.AiOverallScore == null).Should().BeTrue();
        }

        [Fact]
        public async Task GetDashboardSummaryAsync_Interviewer_FiltersToAssignedInterviews()
        {
            // Arrange
            var interviewerId = 99;
            var otherInterviewerId = 100;
            var myInterview = await CreateTestInterviewAsync(i => i.AssignInterviewer(interviewerId));
            var otherInterview = await CreateTestInterviewAsync(i => i.AssignInterviewer(otherInterviewerId));

            SetupUserContext("Interviewer", interviewerId);

            // Act
            var result = await _service.GetDashboardSummaryAsync(new InterviewDashboardFilterParams
            {
                PageNumber = 1,
                PageSize = 10
            });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();
            result.Items[0].Id.Should().Be(myInterview.Id);
        }

        [Fact]
        public async Task GetDashboardSummaryAsync_SortByAiOverallScoreDescending_PlacesNullsLast()
        {
            // Arrange
            SetupUserContext("Admin", 1);
            var scoredLow = await CreateTestInterviewAsync(i => i.SetAggregationResult(5.0m, InterviewRecommendation.Consider));
            var scoredHigh = await CreateTestInterviewAsync(i => i.SetAggregationResult(9.0m, InterviewRecommendation.Pass));
            var unscored = await CreateTestInterviewAsync(i => i.SetAggregationResult(null, null));

            // Act
            var result = await _service.GetDashboardSummaryAsync(new InterviewDashboardFilterParams
            {
                SortBy = "aioverallscore",
                IsDescending = true,
                PageNumber = 1,
                PageSize = 10
            });

            // Assert
            result.Items.Should().HaveCount(3);
            result.Items[0].Id.Should().Be(scoredHigh.Id);
            result.Items[1].Id.Should().Be(scoredLow.Id);
            result.Items[2].Id.Should().Be(unscored.Id); // null is last!
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ReturnsCorrectKpiCounts()
        {
            // Arrange
            SetupUserContext("HR", 2);
            var completedPass = await CreateTestInterviewAsync(i =>
            {
                i.MarkInProgress();
                i.SetAggregationResult(8.5m, InterviewRecommendation.Pass);
                i.Complete();
            });
            var scheduled = await CreateTestInterviewAsync(i => i.Invite(TimeSpan.FromDays(2)));

            // Act
            var stats = await _service.GetDashboardStatsAsync();

            // Assert
            stats.TotalInterviews.Should().Be(2);
            stats.Completed.Should().Be(1);
            stats.RecommendedPass.Should().Be(1);
            stats.PendingDecisions.Should().Be(1); // completed without decision
            stats.Decided.Should().Be(0);
        }

        [Fact]
        public async Task GetAvailableInterviewersAsync_ReturnsOnlyInterviewerRoles()
        {
            // Arrange
            var interviewer = new ApplicationUser("int1@hrms.com", "hash", UserRole.Interviewer);
            var hr = new ApplicationUser("hr@hrms.com", "hash", UserRole.HR);
            var candidate = new ApplicationUser("cand@hrms.com", "hash", UserRole.Candidate);

            _context.Users.AddRange(interviewer, hr, candidate);
            await _context.SaveChangesAsync();

            // Act
            var list = await _service.GetAvailableInterviewersAsync();

            // Assert
            list.Should().Contain(u => u.Id == interviewer.Id && u.Email == "int1@hrms.com");
            list.Should().NotContain(u => u.Id == hr.Id);
            list.Should().NotContain(u => u.Id == candidate.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_MapsCandidateAndJobDetails()
        {
            // Arrange
            SetupUserContext("Admin", 1);
            var interview = await CreateTestInterviewAsync(i =>
            {
                i.SetAggregationResult(9.0m, InterviewRecommendation.Pass, "Top candidate");
            });

            // Act
            var result = await _service.GetByIdAsync(interview.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(interview.Id);
            result.CandidateName.Should().Be("Candidate");
            result.CandidateEmail.Should().Be("c@test.com");
            result.JobTitle.Should().Be("Developer");
            result.ApplicationStatus.Should().Be("Applied");
            result.AiOverallScore.Should().Be(9.0m);
            result.Recommendation.Should().Be("Pass");
        }

        [Fact]
        public async Task CreateScreeningDecisionAsync_WhenOverrideWithoutReason_ThrowsBusinessRuleException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i =>
            {
                i.SetAggregationResult(8.5m, InterviewRecommendation.Pass, "Strong candidate");
            });

            var dto = new ScreeningDecisionCreateDto
            {
                DecisionType = ScreeningDecisionType.Reject,
                OverrideReason = null,
                Notes = "Rejecting despite Pass"
            };

            // Act
            var act = () => _service.CreateScreeningDecisionAsync(interview.Id, dto, 1, "Admin");

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Bắt buộc phải nhập lý do khi đưa ra quyết định khác với gợi ý sơ bộ của hệ thống.*");
        }

        [Fact]
        public async Task CreateScreeningDecisionAsync_WhenAdvanceToInDepth_TransitionsApplicationToInterviewing()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i =>
            {
                i.SetAggregationResult(8.5m, InterviewRecommendation.Pass, "Strong candidate");
            });

            var app = await _context.Applications.FindAsync(interview.ApplicationId);
            app!.TransitionTo(ApplicationStatus.Screening);
            await _context.SaveChangesAsync();

            var dto = new ScreeningDecisionCreateDto
            {
                DecisionType = ScreeningDecisionType.AdvanceToInDepth,
                Notes = "Chuyển sang phỏng vấn vòng 2"
            };

            // Act
            var result = await _service.CreateScreeningDecisionAsync(interview.Id, dto, 1, "HR");

            // Assert
            result.Should().NotBeNull();
            result.DecisionType.Should().Be("AdvanceToInDepth");
            result.SystemRecommendation.Should().Be("Pass");

            var updatedApp = await _context.Applications.FindAsync(interview.ApplicationId);
            updatedApp!.Status.Should().Be(ApplicationStatus.Interviewing);

            var savedDecisions = await _context.ScreeningDecisions.Where(d => d.InterviewId == interview.Id).ToListAsync();
            savedDecisions.Should().HaveCount(1);
            savedDecisions[0].DecisionType.Should().Be(ScreeningDecisionType.AdvanceToInDepth);
        }

        [Fact]
        public async Task CreateScreeningDecisionAsync_WhenReject_TransitionsApplicationToRejected()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync(i =>
            {
                i.SetAggregationResult(3.0m, InterviewRecommendation.Reject, "Weak candidate");
            });

            var app = await _context.Applications.FindAsync(interview.ApplicationId);
            app!.TransitionTo(ApplicationStatus.Screening);
            await _context.SaveChangesAsync();

            var dto = new ScreeningDecisionCreateDto
            {
                DecisionType = ScreeningDecisionType.Reject,
                Notes = "Không đạt yêu cầu"
            };

            // Act
            var result = await _service.CreateScreeningDecisionAsync(interview.Id, dto, 1, "HR");

            // Assert
            result.Should().NotBeNull();
            result.DecisionType.Should().Be("Reject");

            var updatedApp = await _context.Applications.FindAsync(interview.ApplicationId);
            updatedApp!.Status.Should().Be(ApplicationStatus.Rejected);
        }

        [Fact]
        public async Task CreateScreeningDecisionAsync_SendsNotificationsToStakeholders()
        {
            // Arrange
            var interviewer = new ApplicationUser("interviewer@hrms.com", "hash", UserRole.Interviewer);
            var candidateUser = new ApplicationUser("candidate@hrms.com", "hash", UserRole.Candidate);
            _context.Users.AddRange(interviewer, candidateUser);
            await _context.SaveChangesAsync();

            var interview = await CreateTestInterviewAsync(i =>
            {
                i.SetAggregationResult(8.5m, InterviewRecommendation.Pass, "Strong candidate");
            });

            interview.AssignInterviewer(interviewer.Id);

            var app = await _context.Applications.Include(a => a.Candidate).FirstOrDefaultAsync(a => a.Id == interview.ApplicationId);
            app!.Candidate.UserId = candidateUser.Id;
            app.TransitionTo(ApplicationStatus.Screening);
            await _context.SaveChangesAsync();

            var dto = new ScreeningDecisionCreateDto
            {
                DecisionType = ScreeningDecisionType.AdvanceToInDepth,
                Notes = "Passed screening"
            };

            // Act
            await _service.CreateScreeningDecisionAsync(interview.Id, dto, currentUserId: 99, currentRole: "Admin");

            // Assert
            // Interviewer receives SCREENING_DECISION_RECORDED
            _notificationServiceMock.Verify(n => n.SendToUserAsync(
                interviewer.Id,
                "Quyết định sơ loại mới",
                "SCREENING_DECISION_RECORDED",
                It.IsAny<Dictionary<string, string>>(),
                NotificationType.Info,
                "Interview",
                interview.Id,
                It.IsAny<string?>()), Times.Once);

            // Candidate receives APPLICATION_STATUS_CHANGED
            _notificationServiceMock.Verify(n => n.SendToUserAsync(
                candidateUser.Id,
                "Cập nhật trạng thái ứng tuyển",
                "APPLICATION_STATUS_CHANGED",
                It.IsAny<Dictionary<string, string>>(),
                NotificationType.Success,
                "Application",
                app.Id,
                It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public async Task AssignInterviewerAsync_WhenCancelled_ThrowsBusinessRuleException()
        {
            // Arrange
            var interviewer = new ApplicationUser("interviewer2@hrms.com", "hash", UserRole.Interviewer);
            _context.Users.Add(interviewer);
            await _context.SaveChangesAsync();

            var interview = await CreateTestInterviewAsync(i =>
            {
                i.Cancel();
            });

            // Act
            var act = () => _service.AssignInterviewerAsync(interview.Id, interviewer.Id);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot assign an interviewer to a cancelled interview.*");
        }

        [Fact]
        public async Task AssignInterviewerAsync_WhenValid_AssignsAndSendsNotification()
        {
            // Arrange
            var interviewer = new ApplicationUser("interviewer3@hrms.com", "hash", UserRole.Interviewer);
            _context.Users.Add(interviewer);
            await _context.SaveChangesAsync();

            var interview = await CreateTestInterviewAsync();

            // Act
            var result = await _service.AssignInterviewerAsync(interview.Id, interviewer.Id);

            // Assert
            result.Should().NotBeNull();
            result.InterviewerId.Should().Be(interviewer.Id);

            var dbInterview = await _context.Interviews.FindAsync(interview.Id);
            dbInterview!.InterviewerId.Should().Be(interviewer.Id);

            _notificationServiceMock.Verify(n => n.SendToUserAsync(
                interviewer.Id,
                "Phân công phỏng vấn",
                "INTERVIEW_ASSIGNED_INTERVIEWER",
                It.IsAny<Dictionary<string, string>>(),
                NotificationType.ActionRequired,
                "Interview",
                interview.Id,
                It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public async Task ErasePersonalDataAsync_WhenValid_ErasesPhysicalAudioAndAnonymizesData()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync();
            var question = new InterviewQuestion
            {
                InterviewId = interview.Id,
                Question = "Tell me about your experience",
                OrderIndex = 1
            };
            _context.InterviewQuestions.Add(question);
            await _context.SaveChangesAsync();

            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestionId = question.Id,
                AudioStorageKey = "audio/interviews/1/q1_test.webm",
                AudioDurationSeconds = 60,
                AudioMimeType = "audio/webm",
                CreatedAt = DateTime.UtcNow
            };
            answer.StartTranscribing();
            answer.CompleteTranscription(transcript: "Bản bóc băng gốc của ứng viên", detectedLanguage: "vi", sttDurationSeconds: 60.0);
            answer.UpdateTranscript("Bản bóc băng đã sửa bởi HR", 1);

            var eval = QuestionEvaluation.CreateSucceeded(
                answer.Id,
                versionNumber: 1,
                transcriptSnapshot: "Bản bóc băng gốc của ứng viên",
                level: RubricLevel.Good,
                score: 8.0m,
                analysis: "Phân tích chi tiết ứng viên",
                evidenceQuotes: new List<string> { "kinh nghiệm 3 năm" },
                strengths: new List<string> { "Nắm vững kiến thức" },
                concerns: new List<string> { "Cần thêm kỹ năng mềm" },
                modelName: "gpt-4o-mini",
                promptVersion: "v1");

            answer.Evaluations.Add(eval);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            var dto = new ErasePersonalDataDto
            {
                Reason = "Yêu cầu quyền riêng tư của ứng viên theo Luật Bảo vệ dữ liệu cá nhân (Luật số 91/2025/QH15)"
            };

            // Act
            var result = await _service.ErasePersonalDataAsync(interview.Id, dto, 1, "Admin");

            // Assert
            result.Should().NotBeNull();
            result.InterviewId.Should().Be(interview.Id);
            result.ErasedAnswersCount.Should().Be(1);
            result.ErasedAudioFilesCount.Should().Be(1);
            result.ErasedEvaluationsCount.Should().Be(1);

            // Physical audio delete verified
            _audioStorageServiceMock.Verify(s => s.DeleteAudioAsync("audio/interviews/1/q1_test.webm", It.IsAny<CancellationToken>()), Times.Once);

            // Answer personal data cleaned
            var dbAnswer = await _context.InterviewAnswers.Include(a => a.Evaluations).FirstAsync(a => a.Id == answer.Id);
            dbAnswer.AudioStorageKey.Should().BeEmpty();
            dbAnswer.Transcript.Should().BeNull();
            dbAnswer.EditedTranscript.Should().BeNull();
            dbAnswer.TranscriptSegments.Should().BeNull();

            // Evaluation personal data cleaned but audit scores preserved
            var dbEval = dbAnswer.Evaluations.First();
            dbEval.TranscriptSnapshot.Should().Be("[Đã xóa theo chính sách bảo mật dữ liệu cá nhân]");
            dbEval.Analysis.Should().BeNull();
            dbEval.EvidenceQuotes.Should().BeEmpty();
            dbEval.Strengths.Should().BeEmpty();
            dbEval.Concerns.Should().BeEmpty();
            dbEval.Level.Should().Be(RubricLevel.Good);
            dbEval.Score.Should().Be(8.0m);
            dbEval.Outcome.Should().Be(EvaluationOutcome.Succeeded);

            // Check GetByIdAsync reflects personal data erased
            var interviewDto = await _service.GetByIdAsync(interview.Id);
            interviewDto!.IsPersonalDataErased.Should().BeTrue();
            interviewDto.Questions.First().Answer!.IsAudioDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task ErasePersonalDataAsync_WhenReasonEmpty_ThrowsBusinessRuleException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync();
            var dto = new ErasePersonalDataDto { Reason = "   " };

            // Act
            var act = () => _service.ErasePersonalDataAsync(interview.Id, dto, 1, "Admin");

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Lý do xóa dữ liệu cá nhân không được để trống.*");
        }

        [Fact]
        public async Task GetAudioStreamWithAccessLogAsync_WhenPhysicalAudioDeleted_Throws410Gone()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync();
            var question = new InterviewQuestion { InterviewId = interview.Id, Question = "Q", OrderIndex = 1 };
            _context.InterviewQuestions.Add(question);
            await _context.SaveChangesAsync();

            var storageKey = "audio/interviews/1/erased.webm";
            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestionId = question.Id,
                AudioStorageKey = storageKey,
                AudioDurationSeconds = 10,
                AudioMimeType = "audio/webm"
            };
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Setup audioStorageService to report file does NOT exist
            _audioStorageServiceMock.Setup(s => s.ExistsAsync(storageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var act = () => _service.GetAudioStreamWithAccessLogAsync(storageKey, 1, "Admin", "127.0.0.1");

            // Assert
            var ex = await act.Should().ThrowAsync<BusinessRuleException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Gone);
        }

        [Fact]
        public async Task UpdateTranscriptAsync_WhenPersonalDataErased_ThrowsBusinessRuleException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync();
            var question = new InterviewQuestion { InterviewId = interview.Id, Question = "Q", OrderIndex = 1 };
            _context.InterviewQuestions.Add(question);
            await _context.SaveChangesAsync();

            var answer = new InterviewAnswer
            {
                InterviewId = interview.Id,
                InterviewQuestionId = question.Id,
                AudioStorageKey = "audio/temp.webm",
                AudioDurationSeconds = 10,
                AudioMimeType = "audio/webm"
            };
            answer.AnonymizePersonalData();
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act
            var act = () => _service.UpdateTranscriptAsync(interview.Id, question.Id, "New text", 1, expectedVersion: 1);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Dữ liệu cá nhân của câu trả lời này đã bị xóa*");
        }

        [Fact]
        public async Task GetCandidateSessionAsync_WhenMultipleQuestions_ShouldMaskFutureQuestionsAndStampServedAt()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync();
            interview.MarkInProgress();

            var q1 = new InterviewQuestion { Id = 101, InterviewId = interview.Id, Question = "Question 1", OrderIndex = 1, ThinkSeconds = 20, AnswerSeconds = 60 };
            var q2 = new InterviewQuestion { Id = 102, InterviewId = interview.Id, Question = "Question 2", OrderIndex = 2, ThinkSeconds = 20, AnswerSeconds = 60 };
            var q3 = new InterviewQuestion { Id = 103, InterviewId = interview.Id, Question = "Question 3", OrderIndex = 3, ThinkSeconds = 20, AnswerSeconds = 60 };
            _context.InterviewQuestions.AddRange(q1, q2, q3);
            await _context.SaveChangesAsync();

            // Act
            var session = await _service.GetCandidateSessionAsync(interview.InviteToken);

            // Assert
            session.Questions.Should().HaveCount(3);
            var resQ1 = session.Questions.First(q => q.Id == 101);
            var resQ2 = session.Questions.First(q => q.Id == 102);
            var resQ3 = session.Questions.First(q => q.Id == 103);

            resQ1.IsActive.Should().BeTrue();
            resQ1.Question.Should().Be("Question 1");
            resQ1.ServedAt.Should().NotBeNull();
            resQ1.RemainingThinkSeconds.Should().BeGreaterThan(0);

            // Future questions must be masked and locked
            resQ2.IsActive.Should().BeFalse();
            resQ2.IsLocked.Should().BeTrue();
            resQ2.Question.Should().BeEmpty();

            resQ3.IsActive.Should().BeFalse();
            resQ3.IsLocked.Should().BeTrue();
            resQ3.Question.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCandidateSessionAsync_WhenSessionNonceMismatched_ShouldThrowConflictException()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync();
            interview.MarkInProgress();
            var activeNonce = interview.ActiveSessionNonce;
            await _context.SaveChangesAsync();

            // Act: call with a different nonce
            var mismatchedNonce = Guid.NewGuid();
            var act = () => _service.GetCandidateSessionAsync(interview.InviteToken, mismatchedNonce);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*thiết bị/tab khác*");
        }

        [Fact]
        public async Task GetCandidateSessionAsync_WhenQuestionTimedOut_ShouldAutoExpireAndServeNextQuestion()
        {
            // Arrange
            var interview = await CreateTestInterviewAsync();
            interview.MarkInProgress();

            // Q1 was served 60s ago (exceeds Think 10 + Answer 20 + Buffer 5 = 35s)
            var q1 = new InterviewQuestion
            {
                Id = 201,
                InterviewId = interview.Id,
                Question = "Timed Out Question",
                OrderIndex = 1,
                ThinkSeconds = 10,
                AnswerSeconds = 20,
                ServedAt = DateTime.UtcNow.AddSeconds(-60)
            };
            var q2 = new InterviewQuestion
            {
                Id = 202,
                InterviewId = interview.Id,
                Question = "Next Available Question",
                OrderIndex = 2,
                ThinkSeconds = 15,
                AnswerSeconds = 30
            };
            _context.InterviewQuestions.AddRange(q1, q2);
            await _context.SaveChangesAsync();

            // Act
            var session = await _service.GetCandidateSessionAsync(interview.InviteToken);

            // Assert
            session.CurrentQuestionIndex.Should().Be(1);
            var resQ1 = session.Questions.First(q => q.Id == 201);
            var resQ2 = session.Questions.First(q => q.Id == 202);

            // Q1 must be marked as answered via timeout
            resQ1.HasAnswer.Should().BeTrue();
            resQ1.IsActive.Should().BeFalse();

            // Q2 must now be active and unmasked
            resQ2.IsActive.Should().BeTrue();
            resQ2.IsLocked.Should().BeFalse();
            resQ2.Question.Should().Be("Next Available Question");
            resQ2.ServedAt.Should().NotBeNull();

            // Verify database has timeout answer
            var dbAnswer = await _context.InterviewAnswers.FirstOrDefaultAsync(a => a.InterviewQuestionId == 201);
            dbAnswer.Should().NotBeNull();
            dbAnswer!.Flags.Should().Contain("TimedOut");
        }
    }
}
