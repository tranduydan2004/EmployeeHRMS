using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;
using EmployeeHRMS.Api.Services;
using EmployeeHRMS.Api.Services.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace EmployeeHRMS.Tests.Services
{
    public class TranscriptionWorkerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly DbContextOptions<AppDbContext> _dbOptions;
        private readonly Mock<IAudioStorageService> _storageMock;
        private readonly Mock<ISpeechToTextService> _sttMock;
        private readonly Mock<IOpenAiScoringService> _scoringMock;
        private readonly Mock<INotificationService> _notificationMock;
        private readonly Mock<ILogger<TranscriptionWorker>> _loggerMock;
        private readonly Channel<AudioTranscriptionJob> _channel;
        private readonly Mock<IAudioTranscriptionQueue> _queueMock;
        private readonly ITestOutputHelper _output;

        public TranscriptionWorkerTests(ITestOutputHelper output)
        {
            _output = output;
            var dbName = Guid.NewGuid().ToString();
            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            _context = new AppDbContext(_dbOptions);
            _storageMock = new Mock<IAudioStorageService>();
            _sttMock = new Mock<ISpeechToTextService>();
            _scoringMock = new Mock<IOpenAiScoringService>();
            _notificationMock = new Mock<INotificationService>();
            _loggerMock = new Mock<ILogger<TranscriptionWorker>>();

            _loggerMock.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var logLevel = (LogLevel)invocation.Arguments[0];
                    var state = invocation.Arguments[2];
                    var ex = invocation.Arguments[3] as Exception;
                    _output.WriteLine($"[{logLevel}] {state} {(ex != null ? ex.ToString() : "")}");
                }));

            _channel = Channel.CreateUnbounded<AudioTranscriptionJob>();
            _queueMock = new Mock<IAudioTranscriptionQueue>();
            _queueMock.Setup(q => q.Reader).Returns(_channel.Reader);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private IServiceProvider BuildServiceProvider(
            bool includeScoring = false,
            bool includeNotification = false,
            ScoringOptions? scoringOptions = null,
            SpeechMetricsOptions? metricsOptions = null)
        {
            var recoveryMock = new Mock<ITranscriptionRecoveryService>();
            recoveryMock.Setup(r => r.RecoverZombieJobsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EmployeeHRMS.Api.DTOs.TranscriptionRecoveryResponseDto());

            var services = new ServiceCollection();
            services.AddScoped(_ => new AppDbContext(_dbOptions));
            services.AddScoped(_ => _storageMock.Object);
            services.AddScoped(_ => _sttMock.Object);
            services.AddScoped(_ => recoveryMock.Object);

            if (includeScoring)
            {
                services.AddScoped(_ => _scoringMock.Object);
            }

            if (includeNotification)
            {
                services.AddScoped(_ => _notificationMock.Object);
            }

            services.AddSingleton(Options.Create(scoringOptions ?? new ScoringOptions()));
            services.AddSingleton(Options.Create(metricsOptions ?? new SpeechMetricsOptions()));

            return services.BuildServiceProvider();
        }

        private async Task RunWorkerAsync(IServiceProvider provider)
        {
            _channel.Writer.Complete();

            var worker = new TranscriptionWorker(
                _queueMock.Object,
                provider.GetRequiredService<IServiceScopeFactory>(),
                _loggerMock.Object);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StartAsync(cts.Token);
            if (worker.ExecuteTask != null)
            {
                await worker.ExecuteTask;
            }
        }

        [Fact]
        public async Task ProcessJob_WhenTranscriptionSucceedsAndScoringNotRegistered_UpdatesAnswerToScoring()
        {
            // Arrange
            var interview = new Interview
            {
                Id = 10,
                ApplicationId = 100,
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();
            interview.MarkInProgress();

            var question = new InterviewQuestion
            {
                Id = 20,
                InterviewId = 10,
                Question = "Tell me about yourself",
                Category = QuestionCategory.Behavioral
            };
            var answer = new InterviewAnswer
            {
                Id = 1,
                InterviewId = 10,
                InterviewQuestionId = 20,
                AudioStorageKey = "audio/interviews/10/20_test.webm",
                AudioDurationSeconds = 45
            };
            _context.Interviews.Add(interview);
            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            var fakeAudioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));
            _storageMock
                .Setup(s => s.GetAudioStreamAsync(answer.AudioStorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeAudioStream);

            _sttMock
                .Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), "20_test.webm", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TranscriptionResult
                {
                    Text = "Tôi có 3 năm kinh nghiệm lập trình C# và .NET",
                    SegmentsJson = "{\"words\":[{\"word\":\"Tôi\",\"start\":0.1,\"end\":0.3}]}",
                    DetectedLanguage = "vi",
                    DurationSeconds = 45.0
                });

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 1,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: false);
            await RunWorkerAsync(provider);

            // Assert
            var updatedAnswer = await _context.InterviewAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == 1);
            updatedAnswer.Should().NotBeNull();
            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Scoring);
            updatedAnswer.Transcript.Should().Be("Tôi có 3 năm kinh nghiệm lập trình C# và .NET");
            updatedAnswer.TranscriptSegments.Should().Contain("words");
            updatedAnswer.ProcessingStartedAt.Should().NotBeNull();
            updatedAnswer.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task ProcessJob_WhenSttFails_UpdatesAnswerToFailed()
        {
            // Arrange
            var interview = new Interview
            {
                Id = 10,
                ApplicationId = 100,
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();
            interview.MarkInProgress();

            var question = new InterviewQuestion
            {
                Id = 21,
                InterviewId = 10,
                Question = "Tell me about C#",
                Category = QuestionCategory.Technical
            };
            var answer = new InterviewAnswer
            {
                Id = 2,
                InterviewId = 10,
                InterviewQuestionId = 21,
                AudioStorageKey = "audio/interviews/10/21_error.webm",
                AudioDurationSeconds = 30
            };
            _context.Interviews.Add(interview);
            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            var fakeAudioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));
            _storageMock
                .Setup(s => s.GetAudioStreamAsync(answer.AudioStorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeAudioStream);

            _sttMock
                .Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), "21_error.webm", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Whisper API 429 Too Many Requests"));

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 2,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: false);
            await RunWorkerAsync(provider);

            // Assert
            var updatedAnswer = await _context.InterviewAnswers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == 2);
            updatedAnswer.Should().NotBeNull();
            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Failed);
            updatedAnswer.FailedStage.Should().Be(AudioProcessingStage.Transcription);
            updatedAnswer.ErrorMessage.Should().Contain("Whisper API 429 Too Many Requests");
            updatedAnswer.Transcript.Should().BeNull();
        }

        [Fact]
        public async Task ProcessJob_WhenAnswerNotFound_SkipsGracefullyWithoutCrashing()
        {
            // Arrange
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 999,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: false);

            // Act
            await RunWorkerAsync(provider);

            // Assert
            _sttMock.Verify(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessJob_WhenAnswerAlreadyCompleted_SkipsDuplicateJob()
        {
            // Arrange
            var answer = new InterviewAnswer
            {
                Id = 3,
                InterviewId = 10,
                InterviewQuestionId = 22,
                AudioStorageKey = "audio/interviews/10/22_completed.webm",
                AudioDurationSeconds = 60
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Existing transcript");
            answer.CompleteScoring();

            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 3,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: false);

            // Act
            await RunWorkerAsync(provider);

            // Assert
            _sttMock.Verify(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            var unchangedAnswer = await _context.InterviewAnswers.FindAsync(3);
            unchangedAnswer!.Status.Should().Be(AudioProcessingStatus.Completed);
            unchangedAnswer.Transcript.Should().Be("Existing transcript");
        }

        [Fact]
        public async Task ProcessJob_WhenTranscriptionAndScoringSucceed_UpdatesAnswerToCompletedAndCreatesQuestionEvaluation()
        {
            // Arrange
            var interview = new Interview
            {
                Id = 11,
                ApplicationId = 101,
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();
            interview.MarkInProgress();

            var question = new InterviewQuestion
            {
                Id = 30,
                InterviewId = 11,
                Question = "Giải thích vòng đời của component React?",
                Category = QuestionCategory.Technical,
                ScoringRubric = new ScoringRubric
                {
                    Poor = "Không nêu được hook hoặc lifecycle",
                    Acceptable = "Nêu được useEffect cơ bản",
                    Good = "Giải thích rõ mounting, updating, unmounting",
                    Excellent = "Phân tích sâu memoization, cleanup và reconciliation"
                }
            };
            var answer = new InterviewAnswer
            {
                Id = 10,
                InterviewId = 11,
                InterviewQuestionId = 30,
                AudioStorageKey = "audio/interviews/11/30_react.webm",
                AudioDurationSeconds = 50
            };

            _context.Interviews.Add(interview);
            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            var fakeAudioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));
            _storageMock
                .Setup(s => s.GetAudioStreamAsync(answer.AudioStorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeAudioStream);

            _sttMock
                .Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), "30_react.webm", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TranscriptionResult
                {
                    Text = "Component React có 3 giai đoạn chính: mount, update và unmount. useEffect quản lý side effects và cleanup.",
                    SegmentsJson = "{\"words\":[{\"word\":\"Component\",\"start\":0.1,\"end\":0.5}]}",
                    DetectedLanguage = "vi",
                    DurationSeconds = 50.0
                });

            _scoringMock
                .Setup(s => s.ScoreAnswerAsync(It.IsAny<QuestionScoringRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QuestionScoringResult
                {
                    Level = RubricLevel.Good,
                    Score = 7.5m,
                    Analysis = "Ứng viên nắm vững vòng đời component React và cách sử dụng useEffect.",
                    EvidenceQuotes = new List<string> { "mount, update và unmount", "useEffect quản lý side effects" },
                    Strengths = new List<string> { "Giải thích rõ ràng" },
                    Concerns = new List<string>(),
                    ModelName = "gpt-4o-mini",
                    PromptVersion = "v1.0"
                });

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 10,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: true);
            await RunWorkerAsync(provider);

            // Assert
            var updatedAnswer = await _context.InterviewAnswers
                .AsNoTracking()
                .Include(a => a.Evaluations)
                .Include(a => a.InterviewQuestion)
                .FirstOrDefaultAsync(a => a.Id == 10);

            updatedAnswer.Should().NotBeNull();
            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Completed);
            updatedAnswer.Transcript.Should().Contain("Component React có 3 giai đoạn");
            updatedAnswer.SpeechMetrics.Should().NotBeNull();

            // Kiểm tra QuestionEvaluation (Audit Trail)
            updatedAnswer.Evaluations.Should().HaveCount(1);
            var eval = updatedAnswer.Evaluations.First();
            eval.Outcome.Should().Be(EvaluationOutcome.Succeeded);
            eval.Level.Should().Be(RubricLevel.Good);
            eval.Score.Should().Be(7.5m);
            eval.IsActive.Should().BeTrue();
            eval.VersionNumber.Should().Be(1);
            eval.EvidenceQuotes.Should().Contain("mount, update và unmount");
            eval.ModelName.Should().Be("gpt-4o-mini");

            // Kiểm tra Question snapshot score & feedback
            updatedAnswer.InterviewQuestion!.AiQuestionScore.Should().Be(7.5m);
            updatedAnswer.InterviewQuestion.AiFeedback.Should().Contain("Ứng viên nắm vững");
        }

        [Fact]
        public async Task ProcessJob_WhenAlreadyAtScoringState_SkipsSttAndExecutesScoringDirectly()
        {
            // Arrange
            var interview = new Interview
            {
                Id = 12,
                ApplicationId = 102,
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();
            interview.MarkInProgress();

            var question = new InterviewQuestion
            {
                Id = 31,
                InterviewId = 12,
                Question = "Nêu các nguyên lý SOLID?",
                Category = QuestionCategory.Technical,
                ScoringRubric = new ScoringRubric
                {
                    Poor = "Không nhớ nguyên lý",
                    Acceptable = "Nhớ tên 5 nguyên lý",
                    Good = "Nêu định nghĩa và ví dụ",
                    Excellent = "Phân tích trade-off thực tế"
                }
            };

            var answer = new InterviewAnswer
            {
                Id = 11,
                InterviewId = 12,
                InterviewQuestionId = 31,
                AudioStorageKey = "audio/interviews/12/31_solid.webm",
                AudioDurationSeconds = 40
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("SOLID gồm Single Responsibility, Open-Closed, Liskov, Interface Segregation và Dependency Inversion.");

            _context.Interviews.Add(interview);
            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            _scoringMock
                .Setup(s => s.ScoreAnswerAsync(It.IsAny<QuestionScoringRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QuestionScoringResult
                {
                    Level = RubricLevel.Good,
                    Score = 7.5m,
                    Analysis = "Nêu đủ 5 nguyên lý.",
                    ModelName = "gpt-4o-mini",
                    PromptVersion = "v1.0"
                });

            // Act: Job đưa vào khi answer đã ở trạng thái Scoring (backlog recovery hoặc re-evaluate)
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 11,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: true);
            await RunWorkerAsync(provider);

            // Assert: STT không được gọi lại
            _sttMock.Verify(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            var updatedAnswer = await _context.InterviewAnswers
                .AsNoTracking()
                .Include(a => a.Evaluations)
                .FirstOrDefaultAsync(a => a.Id == 11);

            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Completed);
            updatedAnswer.Evaluations.Should().ContainSingle(e => e.Outcome == EvaluationOutcome.Succeeded && e.Score == 7.5m);
        }

        [Fact]
        public async Task ProcessJob_WhenAnswerIsSilenceOrKnownHallucination_CreatesNoResponseEvaluationAndCompletes()
        {
            // Arrange
            var interview = new Interview
            {
                Id = 13,
                ApplicationId = 103,
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();
            interview.MarkInProgress();

            var question = new InterviewQuestion
            {
                Id = 32,
                InterviewId = 13,
                Question = "Hãy giới thiệu dự án tâm đắc nhất của bạn?",
                Category = QuestionCategory.Behavioral,
                ScoringRubric = new ScoringRubric
                {
                    Poor = "Không nêu dự án",
                    Acceptable = "Nêu dự án cơ bản",
                    Good = "Có vai trò và kết quả",
                    Excellent = "Phân tích tác động và bài học"
                }
            };

            var answer = new InterviewAnswer
            {
                Id = 12,
                InterviewId = 13,
                InterviewQuestionId = 32,
                AudioStorageKey = "audio/interviews/13/32_silence.webm",
                AudioDurationSeconds = 15
            };
            answer.StartTranscribing();
            // Cụm từ ảo giác khi im lặng (KnownHallucination)
            answer.CompleteTranscription("Cảm ơn các bạn đã theo dõi.");

            _context.Interviews.Add(interview);
            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 12,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: true);
            await RunWorkerAsync(provider);

            // Assert: LLM scoring không được gọi vì đã bị chặn bởi heuristic
            _scoringMock.Verify(s => s.ScoreAnswerAsync(It.IsAny<QuestionScoringRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            var updatedAnswer = await _context.InterviewAnswers
                .AsNoTracking()
                .Include(a => a.Evaluations)
                .Include(a => a.InterviewQuestion)
                .FirstOrDefaultAsync(a => a.Id == 12);

            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Completed);
            updatedAnswer.Evaluations.Should().HaveCount(1);

            var eval = updatedAnswer.Evaluations.First();
            eval.Outcome.Should().Be(EvaluationOutcome.NoResponse);
            eval.Reason.Should().Be("KnownHallucination");
            eval.Score.Should().BeNull();
            eval.IsActive.Should().BeTrue();

            updatedAnswer.InterviewQuestion!.AiQuestionScore.Should().Be(0.0m);
            updatedAnswer.InterviewQuestion.AiFeedback.Should().Contain("Không phát hiện câu trả lời hợp lệ");
        }

        [Fact]
        public async Task ProcessJob_WhenQuestionLacksRubric_CreatesNotScorableEvaluationAndCompletes()
        {
            // Arrange
            var interview = new Interview
            {
                Id = 14,
                ApplicationId = 104,
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();
            interview.MarkInProgress();

            // Câu hỏi không có ScoringRubric (D3, D10)
            var question = new InterviewQuestion
            {
                Id = 33,
                InterviewId = 14,
                Question = "Câu hỏi do HR tự gõ chưa gắn rubric",
                Category = QuestionCategory.Situational,
                ScoringRubric = null
            };

            var answer = new InterviewAnswer
            {
                Id = 13,
                InterviewId = 14,
                InterviewQuestionId = 33,
                AudioStorageKey = "audio/interviews/14/33_norubric.webm",
                AudioDurationSeconds = 30
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Tôi xử lý tình huống bằng cách trao đổi trực tiếp với khách hàng.");

            _context.Interviews.Add(interview);
            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 13,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: true);
            await RunWorkerAsync(provider);

            // Assert: LLM scoring không được gọi
            _scoringMock.Verify(s => s.ScoreAnswerAsync(It.IsAny<QuestionScoringRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            var updatedAnswer = await _context.InterviewAnswers
                .AsNoTracking()
                .Include(a => a.Evaluations)
                .Include(a => a.InterviewQuestion)
                .FirstOrDefaultAsync(a => a.Id == 13);

            // D10: Answer vẫn chuyển sang Completed để không block tiến trình
            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Completed);
            updatedAnswer.Evaluations.Should().HaveCount(1);

            var eval = updatedAnswer.Evaluations.First();
            eval.Outcome.Should().Be(EvaluationOutcome.NotScorable);
            eval.Reason.Should().Be("MissingRubric");
            eval.Score.Should().BeNull();
            eval.IsActive.Should().BeTrue();

            updatedAnswer.InterviewQuestion!.AiQuestionScore.Should().BeNull();
            updatedAnswer.InterviewQuestion.AiFeedback.Should().Contain("chưa có barem");
        }

        [Fact]
        public async Task ProcessJob_WhenAllInterviewQuestionsCompleted_TriggersAggregationAndCompletesInterview()
        {
            // Arrange
            var interview = new Interview
            {
                Id = 15,
                ApplicationId = 105,
                InterviewerId = 55,
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();
            interview.MarkInProgress();

            // Q1: Đã Completed từ trước với điểm 8.0
            var q1 = new InterviewQuestion
            {
                Id = 41,
                InterviewId = 15,
                OrderIndex = 1,
                Question = "Câu hỏi 1",
                Category = QuestionCategory.Technical,
                ScoringRubric = new ScoringRubric { Good = "Tot", Excellent = "Xuat sac" }
            };
            var ans1 = new InterviewAnswer
            {
                Id = 21,
                InterviewId = 15,
                InterviewQuestionId = 41,
                AudioStorageKey = "audio/q1.webm"
            };
            ans1.StartTranscribing();
            ans1.CompleteTranscription("Tra loi Q1");
            ans1.CompleteScoring();

            var eval1 = QuestionEvaluation.CreateSucceeded(
                ans1.Id, 1, "Tra loi Q1", RubricLevel.Good, 8.0m, "Phan tich tot",
                new List<string>(), new List<string>(), new List<string>(), "gpt-4o-mini", "v1.0");

            // Q2: Đang ở Scoring, sẽ được worker xử lý trong job này
            var q2 = new InterviewQuestion
            {
                Id = 42,
                InterviewId = 15,
                OrderIndex = 2,
                Question = "Câu hỏi 2",
                Category = QuestionCategory.Behavioral,
                ScoringRubric = new ScoringRubric { Good = "Tot", Excellent = "Xuat sac" }
            };
            var ans2 = new InterviewAnswer
            {
                Id = 22,
                InterviewId = 15,
                InterviewQuestionId = 42,
                AudioStorageKey = "audio/q2.webm"
            };
            ans2.StartTranscribing();
            ans2.CompleteTranscription("Tra loi Q2");

            _context.Interviews.Add(interview);
            _context.InterviewQuestions.AddRange(q1, q2);
            _context.InterviewAnswers.AddRange(ans1, ans2);
            _context.QuestionEvaluations.Add(eval1);
            await _context.SaveChangesAsync();

            _scoringMock
                .Setup(s => s.ScoreAnswerAsync(It.IsAny<QuestionScoringRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QuestionScoringResult
                {
                    Level = RubricLevel.Good,
                    Score = 7.5m,
                    Analysis = "Phân tích Q2 đạt yêu cầu.",
                    ModelName = "gpt-4o-mini",
                    PromptVersion = "v1.0"
                });

            // Act: Đẩy job cho ans2
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 22,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: true, includeNotification: true);
            await RunWorkerAsync(provider);

            // Assert: Toàn bộ câu hỏi đã hoàn tất -> Interview được tổng hợp điểm và Complete (D4)
            var updatedInterview = await _context.Interviews
                .AsNoTracking()
                .Include(i => i.Questions)
                    .ThenInclude(q => q.Answer)
                .FirstOrDefaultAsync(i => i.Id == 15);

            updatedInterview.Should().NotBeNull();
            updatedInterview!.Status.Should().Be(InterviewStatus.Completed);
            updatedInterview.AiOverallScore.Should().NotBeNull();
            // Q1 = 8.0, Q2 = 7.5 -> Avg = 7.75
            updatedInterview.AiOverallScore.Should().Be(7.75m);
            updatedInterview.Recommendation.Should().Be(InterviewRecommendation.Pass);
            updatedInterview.AiOverallSummary.Should().NotBeNullOrEmpty();

            // Kiểm tra gửi thông báo real-time theo D13
            _notificationMock.Verify(n => n.SendToRolesAsync(
                It.Is<IEnumerable<UserRole>>(roles => roles.Contains(UserRole.Admin) && roles.Contains(UserRole.HR)),
                "Phỏng vấn hoàn tất",
                "INTERVIEW_COMPLETED",
                It.Is<Dictionary<string, string>>(d => d["InterviewId"] == "15" && d["Recommendation"] == "Pass"),
                NotificationType.Success,
                "Interview",
                15,
                It.IsAny<string>()), Times.Once);

            _notificationMock.Verify(n => n.SendToUserAsync(
                55,
                "Phỏng vấn hoàn tất",
                "INTERVIEW_COMPLETED",
                It.Is<Dictionary<string, string>>(d => d["InterviewId"] == "15"),
                NotificationType.Success,
                "Interview",
                15,
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessJob_WhenScoringFails_SetsFailedStageToScoring()
        {
            // Arrange
            var interview = new Interview
            {
                Id = 16,
                ApplicationId = 106,
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();
            interview.MarkInProgress();

            var question = new InterviewQuestion
            {
                Id = 43,
                InterviewId = 16,
                Question = "Câu hỏi test lỗi scoring",
                Category = QuestionCategory.Technical,
                ScoringRubric = new ScoringRubric { Good = "Tot" }
            };
            var answer = new InterviewAnswer
            {
                Id = 23,
                InterviewId = 16,
                InterviewQuestionId = 43,
                AudioStorageKey = "audio/q3.webm"
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Bản bóc băng hợp lệ");

            _context.Interviews.Add(interview);
            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            await _context.SaveChangesAsync();

            // Giả lập OpenAI chấm điểm ném exception (D8)
            _scoringMock
                .Setup(s => s.ScoreAnswerAsync(It.IsAny<QuestionScoringRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("OpenAI API 500 Internal Server Error"));

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 23,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: true);
            await RunWorkerAsync(provider);

            // Assert
            var updatedAnswer = await _context.InterviewAnswers
                .AsNoTracking()
                .Include(a => a.Evaluations)
                .FirstOrDefaultAsync(a => a.Id == 23);

            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Failed);
            updatedAnswer.FailedStage.Should().Be(AudioProcessingStage.Scoring);
            updatedAnswer.ErrorMessage.Should().Contain("500 Internal Server Error");

            // Có bản ghi audit Failed được lưu
            updatedAnswer.Evaluations.Should().ContainSingle(e =>
                e.Outcome == EvaluationOutcome.Failed &&
                e.ErrorMessage!.Contains("500 Internal Server Error") &&
                e.IsActive);
        }

        [Fact]
        public async Task ProcessJob_WhenReevaluationFailsOnPreviouslyActiveEvaluation_RevertsToCompletedWithFailedAudit()
        {
            // Arrange (D9: Bảo toàn kết quả chấm cũ khi chấm lại bị lỗi)
            var interview = new Interview
            {
                Id = 17,
                ApplicationId = 107,
                ScheduledDate = DateTime.UtcNow
            };
            interview.Schedule();
            interview.MarkInProgress();

            var question = new InterviewQuestion
            {
                Id = 44,
                InterviewId = 17,
                Question = "Câu hỏi re-evaluate",
                Category = QuestionCategory.Technical,
                ScoringRubric = new ScoringRubric { Good = "Tot", Excellent = "Xuat sac" },
                AiQuestionScore = 7.5m,
                AiFeedback = "Kết quả đánh giá lần 1"
            };

            var answer = new InterviewAnswer
            {
                Id = 24,
                InterviewId = 17,
                InterviewQuestionId = 44,
                AudioStorageKey = "audio/q4.webm"
            };
            answer.StartTranscribing();
            answer.CompleteTranscription("Transcript ban dau");
            answer.CompleteScoring();

            // Bản đánh giá Version 1 thành công đang Active
            var evalV1 = QuestionEvaluation.CreateSucceeded(
                answer.Id, 1, "Transcript ban dau", RubricLevel.Good, 7.5m, "Danh gia tot",
                new List<string>(), new List<string>(), new List<string>(), "gpt-4o-mini", "v1.0");

            // Đổi trạng thái sang Scoring để chuẩn bị re-evaluate
            answer.StartScoring();

            _context.Interviews.Add(interview);
            _context.InterviewQuestions.Add(question);
            _context.InterviewAnswers.Add(answer);
            _context.QuestionEvaluations.Add(evalV1);
            await _context.SaveChangesAsync();

            // LLM ném lỗi trong lần re-evaluate
            _scoringMock
                .Setup(s => s.ScoreAnswerAsync(It.IsAny<QuestionScoringRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("OpenAI scoring request timed out"));

            // Act
            await _channel.Writer.WriteAsync(new AudioTranscriptionJob
            {
                InterviewAnswerId = 24,
                EnqueuedAt = DateTime.UtcNow
            });

            var provider = BuildServiceProvider(includeScoring: true);
            await RunWorkerAsync(provider);

            // Assert: Trạng thái quay về Completed thay vì Failed
            var updatedAnswer = await _context.InterviewAnswers
                .AsNoTracking()
                .Include(a => a.Evaluations)
                .FirstOrDefaultAsync(a => a.Id == 24);

            updatedAnswer!.Status.Should().Be(AudioProcessingStatus.Completed);
            updatedAnswer.ErrorMessage.Should().Contain("timed out");

            // Audit Trail có 2 bản ghi: V1 (Active = true), V2 Failed (Active = false)
            updatedAnswer.Evaluations.Should().HaveCount(2);

            var activeEval = updatedAnswer.Evaluations.Single(e => e.IsActive);
            activeEval.VersionNumber.Should().Be(1);
            activeEval.Outcome.Should().Be(EvaluationOutcome.Succeeded);
            activeEval.Score.Should().Be(7.5m);

            var failedEval = updatedAnswer.Evaluations.Single(e => !e.IsActive);
            failedEval.VersionNumber.Should().Be(2);
            failedEval.Outcome.Should().Be(EvaluationOutcome.Failed);
            failedEval.ErrorMessage.Should().Contain("timed out");
        }
    }
}
