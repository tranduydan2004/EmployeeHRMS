using System;
using System.Collections.Generic;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.Models
{
    public class InterviewDomainTests
    {
        [Fact]
        public void Complete_FromInProgress_ShouldTransitionToCompleted()
        {
            // Arrange
            var interview = new Interview();
            interview.Invite(TimeSpan.FromDays(2));
            interview.MarkInProgress();

            // Act
            interview.Complete("Tóm tắt từ AI", 8.5m, InterviewRecommendation.Pass);

            // Assert
            interview.Status.Should().Be(InterviewStatus.Completed);
            interview.AiOverallScore.Should().Be(8.5m);
            interview.AiOverallSummary.Should().Be("Tóm tắt từ AI");
            interview.Recommendation.Should().Be(InterviewRecommendation.Pass);
        }

        [Fact]
        public void Complete_FromExpired_ShouldTransitionToCompleted()
        {
            // Arrange
            var interview = new Interview();
            interview.Invite(TimeSpan.FromDays(2));
            interview.MarkExpired();

            // Act
            interview.Complete("Hoàn tất sau khi hết hạn", 5.0m, InterviewRecommendation.Consider);

            // Assert
            interview.Status.Should().Be(InterviewStatus.Completed);
            interview.Recommendation.Should().Be(InterviewRecommendation.Consider);
        }

        [Fact]
        public void Complete_CalledMultipleTimes_ShouldBeIdempotentNoOp()
        {
            // Arrange
            var interview = new Interview();
            interview.Invite(TimeSpan.FromDays(2));
            interview.MarkInProgress();
            interview.Complete("Lần 1", 8.0m, InterviewRecommendation.Pass);

            // Act
            interview.Complete("Lần 2", 9.0m, InterviewRecommendation.Consider);

            // Assert (D4: no-op, không ném exception, giữ nguyên trạng thái)
            interview.Status.Should().Be(InterviewStatus.Completed);
            interview.AiOverallScore.Should().Be(8.0m);
            interview.AiOverallSummary.Should().Be("Lần 1");
            interview.Recommendation.Should().Be(InterviewRecommendation.Pass);
        }

        [Fact]
        public void Complete_FromScheduled_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var interview = new Interview();
            interview.Invite(TimeSpan.FromDays(2));

            // Act & Assert (D4: cấm chuyển trực tiếp từ Scheduled -> Completed)
            var act = () => interview.Complete();
            act.Should().Throw<BusinessRuleException>()
                .WithMessage("*Only InProgress or Expired interviews can be completed*");
        }

        [Fact]
        public void Complete_FromCancelled_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var interview = new Interview();
            interview.Invite(TimeSpan.FromDays(2));
            interview.Cancel();

            // Act & Assert (D4: cấm chuyển từ Cancelled -> Completed)
            var act = () => interview.Complete();
            act.Should().Throw<BusinessRuleException>()
                .WithMessage("*Only InProgress or Expired interviews can be completed*");
        }

        [Fact]
        public void SetAggregationResult_ShouldUpdateScoreRecommendationAndSummary()
        {
            // Arrange
            var interview = new Interview();

            // Act
            interview.SetAggregationResult(7.5m, InterviewRecommendation.Pass, "Đạt yêu cầu");

            // Assert
            interview.AiOverallScore.Should().Be(7.5m);
            interview.Recommendation.Should().Be(InterviewRecommendation.Pass);
            interview.AiOverallSummary.Should().Be("Đạt yêu cầu");
        }
    }

    public class QuestionEvaluationDomainTests
    {
        [Fact]
        public void CreateSucceeded_ShouldPopulateCorrectFieldsAndSetActive()
        {
            // Arrange & Act
            var eval = QuestionEvaluation.CreateSucceeded(
                interviewAnswerId: 10,
                versionNumber: 1,
                transcriptSnapshot: "Tôi có kinh nghiệm tối ưu EF Core query với AsNoTracking.",
                level: RubricLevel.Good,
                score: 7.5m,
                analysis: "Ứng viên nắm vững cơ chế tracking của EF Core.",
                evidenceQuotes: new List<string> { "kinh nghiệm tối ưu EF Core" },
                strengths: new List<string> { "Hiểu rõ hiệu năng" },
                concerns: new List<string>(),
                modelName: "gpt-4o-mini",
                promptVersion: "v1.0",
                evaluatedByUserId: 5);

            // Assert
            eval.InterviewAnswerId.Should().Be(10);
            eval.VersionNumber.Should().Be(1);
            eval.Outcome.Should().Be(EvaluationOutcome.Succeeded);
            eval.Level.Should().Be(RubricLevel.Good);
            eval.Score.Should().Be(7.5m);
            eval.IsActive.Should().BeTrue();
            eval.ModelName.Should().Be("gpt-4o-mini");
            eval.PromptVersion.Should().Be("v1.0");
            eval.EvaluatedByUserId.Should().Be(5);
            eval.EvidenceQuotes.Should().ContainSingle();
        }

        [Fact]
        public void CreateNotScorable_ShouldSetScoreNull_AndPreserveReason()
        {
            // Arrange & Act
            var eval = QuestionEvaluation.CreateNotScorable(
                interviewAnswerId: 10,
                versionNumber: 2,
                transcriptSnapshot: "Câu trả lời hoàn toàn lạc đề.",
                reason: "OffTopic",
                analysis: "Ứng viên không trả lời vào trọng tâm câu hỏi kỹ thuật.",
                modelName: "gpt-4o-mini",
                promptVersion: "v1.0");

            // Assert
            eval.Outcome.Should().Be(EvaluationOutcome.NotScorable);
            eval.Level.Should().BeNull();
            eval.Score.Should().BeNull();
            eval.Reason.Should().Be("OffTopic");
            eval.IsActive.Should().BeTrue();
        }

        [Fact]
        public void CreateNoResponse_WhenSilenceOrHallucination_ShouldSetOutcomeNoResponse()
        {
            // Arrange & Act
            var eval = QuestionEvaluation.CreateNoResponse(
                interviewAnswerId: 10,
                versionNumber: 1,
                transcriptSnapshot: "Cảm ơn các bạn đã theo dõi",
                reason: "KnownHallucination",
                modelName: "gpt-4o-mini",
                promptVersion: "v1.0");

            // Assert
            eval.Outcome.Should().Be(EvaluationOutcome.NoResponse);
            eval.Reason.Should().Be("KnownHallucination");
            eval.Score.Should().BeNull();
            eval.IsActive.Should().BeTrue();
        }

        [Fact]
        public void CreateFailed_ForReevaluation_ShouldSetIsActiveFalse()
        {
            // Arrange & Act (D9: Khi re-evaluate bị lỗi thì lưu audit trail nhưng isActive = false)
            var eval = QuestionEvaluation.CreateFailed(
                interviewAnswerId: 10,
                versionNumber: 3,
                transcriptSnapshot: "Text snapshot",
                errorMessage: "OpenAI rate limit exceeded",
                modelName: "gpt-4o-mini",
                promptVersion: "v1.0",
                isActive: false);

            // Assert
            eval.Outcome.Should().Be(EvaluationOutcome.Failed);
            eval.ErrorMessage.Should().Be("OpenAI rate limit exceeded");
            eval.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Deactivate_ShouldChangeIsActiveToFalse()
        {
            // Arrange
            var eval = QuestionEvaluation.CreateSucceeded(1, 1, "Snapshot", RubricLevel.Acceptable, 5.0m, "OK", null, null, null, "gpt-4o-mini", "v1.0");

            // Act
            eval.Deactivate();

            // Assert
            eval.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Confirm_ShouldRecordUserIdAndTimestamp()
        {
            // Arrange
            var eval = QuestionEvaluation.CreateSucceeded(1, 1, "Snapshot", RubricLevel.Excellent, 10.0m, "Xuất sắc", null, null, null, "gpt-4o-mini", "v1.0");

            // Act
            eval.Confirm(99);

            // Assert
            eval.ConfirmedByUserId.Should().Be(99);
            eval.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
    }

    public class InterviewAnswerDomainTests
    {
        [Fact]
        public void CompleteTranscription_WithSpeechMetrics_ShouldUpdateMetricsAndTransitionToScoring()
        {
            // Arrange
            var answer = new InterviewAnswer();
            answer.StartTranscribing();

            var metrics = new SpeechMetrics
            {
                Wpm = 145.5,
                SpeechDurationSeconds = 42.0,
                WordCount = 102,
                PauseVariationCv = 0.25,
                AveragePauseSeconds = 0.45,
                PauseCount = 12,
                IsRhythmMonotonous = false
            };

            // Act
            answer.CompleteTranscription(
                transcript: "Đây là câu trả lời của tôi",
                transcriptSegments: "[{\"text\":\"Đây là\"}]",
                detectedLanguage: "vi",
                sttDurationSeconds: 43.5,
                speechMetrics: metrics);

            // Assert
            answer.Status.Should().Be(AudioProcessingStatus.Scoring);
            answer.Transcript.Should().Be("Đây là câu trả lời của tôi");
            answer.DetectedLanguage.Should().Be("vi");
            answer.SttDurationSeconds.Should().Be(43.5);
            answer.SpeechMetrics.Should().NotBeNull();
            answer.SpeechMetrics!.Wpm.Should().Be(145.5);
        }

        [Fact]
        public void UpdateTranscript_ShouldSetEditedTranscriptAndAuditUser()
        {
            // Arrange
            var answer = new InterviewAnswer();

            // Act
            answer.UpdateTranscript("Đoạn text đã được sửa bởi HR", 42);

            // Assert
            answer.EditedTranscript.Should().Be("Đoạn text đã được sửa bởi HR");
            answer.LastEditedByUserId.Should().Be(42);
            answer.LastEditedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            answer.GetEffectiveTranscript().Should().Be("Đoạn text đã được sửa bởi HR");
        }

        [Fact]
        public void GetEffectiveTranscript_WhenEditedTranscriptNull_ShouldReturnRawTranscript()
        {
            // Arrange
            var answer = new InterviewAnswer();
            answer.StartTranscribing();
            answer.CompleteTranscription("Bản gốc Whisper");

            // Act & Assert
            answer.GetEffectiveTranscript().Should().Be("Bản gốc Whisper");
        }

        [Fact]
        public void MarkFailed_WithStage_ShouldRecordStageAndErrorMessage()
        {
            // Arrange
            var answer = new InterviewAnswer();

            // Act
            answer.MarkFailed(AudioProcessingStage.Scoring, "JSON parse failure");

            // Assert (D8)
            answer.Status.Should().Be(AudioProcessingStatus.Failed);
            answer.FailedStage.Should().Be(AudioProcessingStage.Scoring);
            answer.ErrorMessage.Should().Be("JSON parse failure");
        }

        [Fact]
        public void RevertToCompletedAfterFailedReevaluation_ShouldReturnToCompletedStatus()
        {
            // Arrange
            var answer = new InterviewAnswer();
            answer.StartTranscribing();
            answer.CompleteTranscription("Text");
            answer.CompleteScoring();

            answer.StartScoring(); // Re-eval claim
            answer.Status.Should().Be(AudioProcessingStatus.Scoring);

            // Act (D9)
            answer.RevertToCompletedAfterFailedReevaluation("Lỗi khi chấm lại");

            // Assert
            answer.Status.Should().Be(AudioProcessingStatus.Completed);
            answer.ErrorMessage.Should().Be("Lỗi khi chấm lại");
        }
    }
}
