using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;
using EmployeeHRMS.Api.Services;
using EmployeeHRMS.Api.Services.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class JobPostingServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IJdGenerationService> _mockJdService;
        private readonly Mock<IQuestionBankService> _mockQbService;
        private readonly AudioInterviewOptions _options;
        private readonly JobPostingService _service;

        public JobPostingServiceTests()
        {
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(dbOptions);

            var mockLogger = new Mock<ILogger<EventLogger>>();
            var eventLogger = new EventLogger(mockLogger.Object);

            _mockJdService = new Mock<IJdGenerationService>();
            _mockQbService = new Mock<IQuestionBankService>();

            _options = new AudioInterviewOptions
            {
                DefaultThinkSeconds = 20,
                DefaultAnswerSeconds = 90,
                MinThinkSeconds = 5,
                MaxThinkSeconds = 40,
                MinAnswerSeconds = 10,
                MaxAnswerSeconds = 120,
                TechnicalJuniorThinkSeconds = 20,
                TechnicalJuniorAnswerSeconds = 60,
                TechnicalSeniorThinkSeconds = 30,
                TechnicalSeniorAnswerSeconds = 90,
                SituationalThinkSeconds = 20,
                SituationalAnswerSeconds = 90,
                BehavioralThinkSeconds = 15,
                BehavioralAnswerSeconds = 60,
                CulturalFitThinkSeconds = 15,
                CulturalFitAnswerSeconds = 45
            };

            var iOptions = Options.Create(_options);

            _service = new JobPostingService(
                _context,
                eventLogger,
                _mockJdService.Object,
                _mockQbService.Object,
                iOptions);

            SeedData();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SeedData()
        {
            var dept = new Department { Id = 1, Name = "Engineering" };
            _context.Departments.Add(dept);

            var draftJob = new JobPosting
            {
                Id = 1,
                Title = "Backend Engineer",
                DepartmentId = 1,
                Status = JobPostingStatus.Draft,
                Level = JobLevel.Senior,
                JdContent = new JdContent
                {
                    Intro = "Looking for Senior .NET",
                    Responsibilities = new List<string> { "Coding" },
                    MustHave = new List<string> { "C#" }
                }
            };

            var approvedJob = new JobPosting
            {
                Id = 2,
                Title = "Frontend Developer",
                DepartmentId = 1,
                Status = JobPostingStatus.Approved,
                Level = JobLevel.Junior,
                JdContent = new JdContent
                {
                    Intro = "Looking for Junior React",
                    Responsibilities = new List<string> { "UI Coding" },
                    MustHave = new List<string> { "React" }
                }
            };

            var question1 = new QuestionBankItem
            {
                Id = 10,
                JobPostingId = 2,
                Question = "Tell us about React hooks.",
                Category = QuestionCategory.Technical,
                Difficulty = JobLevel.Junior,
                ThinkSeconds = 20,
                AnswerSeconds = 60,
                OrderIndex = 0
            };

            var question2 = new QuestionBankItem
            {
                Id = 11,
                JobPostingId = 2,
                Question = "Tell us about conflict resolution.",
                Category = QuestionCategory.Behavioral,
                Difficulty = JobLevel.Junior,
                ThinkSeconds = null, // Will resolve to default 20/90
                AnswerSeconds = null,
                OrderIndex = 1
            };

            var closedJob = new JobPosting
            {
                Id = 3,
                Title = "DevOps Engineer",
                DepartmentId = 1,
                Status = JobPostingStatus.Closed
            };

            var question3 = new QuestionBankItem
            {
                Id = 12,
                JobPostingId = 3,
                Question = "Explain CI/CD pipelines.",
                Category = QuestionCategory.Technical,
                Difficulty = JobLevel.Middle,
                ThinkSeconds = 25,
                AnswerSeconds = 75,
                OrderIndex = 0
            };

            var publishedJob = new JobPosting
            {
                Id = 4,
                Title = "Fullstack .NET & React Engineer",
                DepartmentId = 1,
                Status = JobPostingStatus.Published,
                Level = JobLevel.Senior,
                WorkMode = WorkMode.Hybrid,
                CoreSkills = new List<string> { "C#", "React", "PostgreSQL" },
                JdContent = new JdContent
                {
                    Intro = "Looking for Senior Fullstack",
                    Responsibilities = new List<string> { "Fullstack dev" },
                    MustHave = new List<string> { "C#", "React" },
                    NiceToHave = new List<string> { "Docker" },
                    Benefits = new List<string> { "Competitive salary" }
                }
            };

            _context.JobPostings.AddRange(draftJob, approvedJob, closedJob, publishedJob);
            _context.QuestionBankItems.AddRange(question1, question2, question3);
            _context.SaveChanges();
        }

        [Fact]
        public async Task ApproveJdAsync_PopulatesSuggestedTimeLimitsForGeneratedQuestions()
        {
            // Arrange
            var generatedQuestions = new List<QuestionBankGenerationResult>
            {
                new()
                {
                    Question = "Tech Junior Question",
                    Category = QuestionCategory.Technical,
                    Difficulty = JobLevel.Junior,
                    ScoringRubric = new ScoringRubric { Excellent = "E", Good = "G", Acceptable = "A", Poor = "P" }
                },
                new()
                {
                    Question = "Tech Senior Question",
                    Category = QuestionCategory.Technical,
                    Difficulty = JobLevel.Senior,
                    ScoringRubric = new ScoringRubric { Excellent = "E", Good = "G", Acceptable = "A", Poor = "P" }
                },
                new()
                {
                    Question = "Situational Question",
                    Category = QuestionCategory.Situational,
                    Difficulty = JobLevel.Senior,
                    ScoringRubric = new ScoringRubric { Excellent = "E", Good = "G", Acceptable = "A", Poor = "P" }
                },
                new()
                {
                    Question = "Behavioral Question",
                    Category = QuestionCategory.Behavioral,
                    Difficulty = JobLevel.Senior,
                    ScoringRubric = new ScoringRubric { Excellent = "E", Good = "G", Acceptable = "A", Poor = "P" }
                },
                new()
                {
                    Question = "CulturalFit Question",
                    Category = QuestionCategory.CulturalFit,
                    Difficulty = JobLevel.Senior,
                    ScoringRubric = new ScoringRubric { Excellent = "E", Good = "G", Acceptable = "A", Poor = "P" }
                }
            };

            _mockQbService.Setup(q => q.GenerateQuestionsAsync(It.IsAny<QuestionBankPromptData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(generatedQuestions);

            // Act
            var result = await _service.ApproveJdAsync(1);

            // Assert
            result.Status.Should().Be("Approved");
            var questions = await _context.QuestionBankItems
                .Where(q => q.JobPostingId == 1)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();

            questions.Should().HaveCount(5);

            // Tech Junior: 20 / 60
            questions[0].ThinkSeconds.Should().Be(20);
            questions[0].AnswerSeconds.Should().Be(60);

            // Tech Senior: 30 / 90
            questions[1].ThinkSeconds.Should().Be(30);
            questions[1].AnswerSeconds.Should().Be(90);

            // Situational: 20 / 90
            questions[2].ThinkSeconds.Should().Be(20);
            questions[2].AnswerSeconds.Should().Be(90);

            // Behavioral: 15 / 60
            questions[3].ThinkSeconds.Should().Be(15);
            questions[3].AnswerSeconds.Should().Be(60);

            // CulturalFit: 15 / 45
            questions[4].ThinkSeconds.Should().Be(15);
            questions[4].AnswerSeconds.Should().Be(45);
        }

        [Fact]
        public async Task GetQuestionsAsync_ReturnsResolvedTimeLimits()
        {
            // Act
            var questions = await _service.GetQuestionsAsync(2);

            // Assert
            questions.Should().HaveCount(2);

            // Question 10 has explicit 20 / 60
            questions[0].Id.Should().Be(10);
            questions[0].ThinkSeconds.Should().Be(20);
            questions[0].AnswerSeconds.Should().Be(60);

            // Question 11 had null / null => resolved to default 20 / 90
            questions[1].Id.Should().Be(11);
            questions[1].ThinkSeconds.Should().Be(20);
            questions[1].AnswerSeconds.Should().Be(90);
        }

        [Fact]
        public async Task UpdateQuestionAsync_ValidInput_UpdatesQuestionAndRubricAndTimeLimits()
        {
            // Arrange
            var dto = new UpdateQuestionBankItemDto
            {
                Question = "Updated Question Title",
                ThinkSeconds = 35,
                AnswerSeconds = 110,
                ScoringRubric = new ScoringRubricDto
                {
                    Excellent = "New Exc",
                    Good = "New Good",
                    Acceptable = "New Acc",
                    Poor = "New Poor"
                }
            };

            // Act
            var updated = await _service.UpdateQuestionAsync(2, 10, dto);

            // Assert
            updated.Id.Should().Be(10);
            updated.Question.Should().Be("Updated Question Title");
            updated.ThinkSeconds.Should().Be(35);
            updated.AnswerSeconds.Should().Be(110);
            updated.ScoringRubric.Should().NotBeNull();
            updated.ScoringRubric!.Excellent.Should().Be("New Exc");

            // Verify in DB
            var inDb = await _context.QuestionBankItems.FindAsync(10);
            inDb!.Question.Should().Be("Updated Question Title");
            inDb.ThinkSeconds.Should().Be(35);
            inDb.AnswerSeconds.Should().Be(110);
            inDb.ScoringRubric!.Good.Should().Be("New Good");
        }

        [Fact]
        public async Task UpdateQuestionAsync_InvalidThinkSeconds_ThrowsBusinessRuleException()
        {
            // Arrange: ThinkSeconds = 45 exceeds max 40
            var dto = new UpdateQuestionBankItemDto
            {
                ThinkSeconds = 45
            };

            // Act
            var act = () => _service.UpdateQuestionAsync(2, 10, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Thời gian suy nghĩ không hợp lệ*");
        }

        [Fact]
        public async Task UpdateQuestionAsync_InvalidAnswerSeconds_ThrowsBusinessRuleException()
        {
            // Arrange: AnswerSeconds = 5 is below min 10
            var dto = new UpdateQuestionBankItemDto
            {
                AnswerSeconds = 5
            };

            // Act
            var act = () => _service.UpdateQuestionAsync(2, 10, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Thời gian trả lời không hợp lệ*");
        }

        [Fact]
        public async Task UpdateQuestionAsync_WhenJobNotFound_ThrowsNotFoundException()
        {
            var dto = new UpdateQuestionBankItemDto { ThinkSeconds = 25 };
            var act = () => _service.UpdateQuestionAsync(999, 10, dto);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateQuestionAsync_WhenQuestionNotFoundInJob_ThrowsNotFoundException()
        {
            // Question 12 belongs to Job 3, not Job 2
            var dto = new UpdateQuestionBankItemDto { ThinkSeconds = 25 };
            var act = () => _service.UpdateQuestionAsync(2, 12, dto);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateQuestionAsync_WhenJobIsClosed_ThrowsBusinessRuleException()
        {
            // Job 3 is Closed
            var dto = new UpdateQuestionBankItemDto { ThinkSeconds = 25 };
            var act = () => _service.UpdateQuestionAsync(3, 12, dto);
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Không thể chỉnh sửa câu hỏi của tin tuyển dụng đã đóng*");
        }

        [Fact]
        public async Task GetPublicByIdAsync_WhenJobIsPublished_ReturnsPublicResponseDtoWithStructuredJdContent()
        {
            // Act
            var result = await _service.GetPublicByIdAsync(4, isManagementUser: false);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(4);
            result.Title.Should().Be("Fullstack .NET & React Engineer");
            result.Status.Should().Be("Published");
            result.DepartmentName.Should().Be("Engineering");
            result.Level.Should().Be("Senior");
            result.WorkMode.Should().Be("Hybrid");
            result.CoreSkills.Should().ContainInOrder("C#", "React", "PostgreSQL");

            result.JdContent.Should().NotBeNull();
            result.JdContent!.Intro.Should().Be("Looking for Senior Fullstack");
            result.JdContent.Responsibilities.Should().Contain("Fullstack dev");
            result.JdContent.MustHave.Should().Contain("C#");
            result.JdContent.NiceToHave.Should().Contain("Docker");
            result.JdContent.Benefits.Should().Contain("Competitive salary");
        }

        [Theory]
        [InlineData(1)] // Draft
        [InlineData(2)] // Approved
        [InlineData(3)] // Closed
        public async Task GetPublicByIdAsync_WhenJobIsNotPublished_AndNonManagementUser_ThrowsNotFoundException(int nonPublishedJobId)
        {
            // Act
            var act = () => _service.GetPublicByIdAsync(nonPublishedJobId, isManagementUser: false);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetPublicByIdAsync_WhenJobIsNotPublished_AndManagementUser_ReturnsDto()
        {
            // Act: Job 1 is Draft, but user is Admin/HR
            var result = await _service.GetPublicByIdAsync(1, isManagementUser: true);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Status.Should().Be("Draft");
            result.JdContent.Should().NotBeNull();
            result.JdContent!.Intro.Should().Be("Looking for Senior .NET");
        }

        [Fact]
        public async Task UpdateJdContentAsync_WhenJobIsDraft_UpdatesBothJdContentAndMetadata()
        {
            // Arrange
            var updateDto = new UpdateJdContentDto
            {
                Intro = "Updated Intro for Senior .NET",
                Responsibilities = new List<string> { "Lead architecture", "Optimize DB" },
                MustHave = new List<string> { "8+ years C#", "PostgreSQL" },
                NiceToHave = new List<string> { "Kubernetes" },
                Benefits = new List<string> { "Health Insurance", "Remote allowance" },
                Title = "Lead .NET Solutions Architect",
                Level = JobLevel.Lead,
                WorkMode = WorkMode.Remote,
                CoreSkills = new List<string> { "C#", "Architecture", "PostgreSQL" },
                YearsOfExperience = 8,
                SalaryMin = 40000000,
                SalaryMax = 70000000,
                Currency = Currency.VND
            };

            // Act: Job 1 is Draft
            var result = await _service.UpdateJdContentAsync(1, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Lead .NET Solutions Architect");
            result.Level.Should().Be("Lead");
            result.WorkMode.Should().Be("Remote");
            result.CoreSkills.Should().ContainInOrder("C#", "Architecture", "PostgreSQL");
            result.YearsOfExperience.Should().Be(8);
            result.SalaryRange.Should().NotBeNull();
            result.SalaryRange!.SalaryMin.Should().Be(40000000);
            result.SalaryRange.SalaryMax.Should().Be(70000000);
            result.SalaryRange.Currency.Should().Be("VND");

            result.JdContent.Should().NotBeNull();
            result.JdContent!.Intro.Should().Be("Updated Intro for Senior .NET");
            result.JdContent.Responsibilities.Should().Contain("Lead architecture");
            result.JdContent.MustHave.Should().Contain("8+ years C#");
            result.JdContent.NiceToHave.Should().Contain("Kubernetes");
            result.JdContent.Benefits.Should().Contain("Remote allowance");
        }

        [Fact]
        public async Task UpdateJdContentAsync_WhenJobIsNotDraft_ThrowsBusinessRuleException()
        {
            // Arrange
            var updateDto = new UpdateJdContentDto
            {
                Intro = "Trying to edit an approved job",
                Responsibilities = new List<string> { "Resp" },
                MustHave = new List<string> { "Skill" }
            };

            // Act: Job 2 is Approved
            var act = () => _service.UpdateJdContentAsync(2, updateDto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*JD content can only be edited when status is Draft*");
        }
    }
}
