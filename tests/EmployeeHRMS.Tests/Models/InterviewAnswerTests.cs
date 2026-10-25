using System;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.Models
{
    public class InterviewAnswerTests
    {
        [Fact]
        public void InitialState_ShouldBePendingWithDefaults()
        {
            var answer = new InterviewAnswer
            {
                InterviewId = 1,
                InterviewQuestionId = 10,
                AudioStorageKey = "audio/interviews/1/q10.webm",
                AudioDurationSeconds = 45,
                AudioMimeType = "audio/webm",
                AudioFileSize = 102400
            };

            answer.Status.Should().Be(AudioProcessingStatus.Pending);
            answer.RetryCount.Should().Be(0);
            answer.ProcessingStartedAt.Should().BeNull();
            answer.CompletedAt.Should().BeNull();
            answer.Transcript.Should().BeNull();
            answer.TranscriptSegments.Should().BeNull();
            answer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            answer.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void StartTranscribing_FromPending_ShouldSucceed()
        {
            var answer = new InterviewAnswer();

            answer.StartTranscribing();

            answer.Status.Should().Be(AudioProcessingStatus.Transcribing);
            answer.ProcessingStartedAt.Should().NotBeNull();
            answer.ProcessingStartedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            answer.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void StartTranscribing_FromFailed_ShouldSucceed()
        {
            var answer = new InterviewAnswer();
            answer.MarkFailed("Network timeout");

            answer.StartTranscribing();

            answer.Status.Should().Be(AudioProcessingStatus.Transcribing);
            answer.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void StartTranscribing_FromInvalidState_ShouldThrow()
        {
            var answer = new InterviewAnswer();
            answer.StartTranscribing();
            answer.CompleteTranscription("Hello world");

            // Now in Scoring state
            var act = () => answer.StartTranscribing();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot start transcribing from state 'Scoring'*");
        }

        [Fact]
        public void CompleteTranscription_FromTranscribing_ShouldTransitionToScoring()
        {
            var answer = new InterviewAnswer();
            answer.StartTranscribing();

            answer.CompleteTranscription("Candidate response text", "{\"words\": []}");

            answer.Status.Should().Be(AudioProcessingStatus.Scoring);
            answer.Transcript.Should().Be("Candidate response text");
            answer.TranscriptSegments.Should().Be("{\"words\": []}");
        }

        [Fact]
        public void CompleteTranscription_FromNonTranscribingState_ShouldThrow()
        {
            var answer = new InterviewAnswer(); // Pending state

            var act = () => answer.CompleteTranscription("Candidate response text");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot complete transcription from state 'Pending'*");
        }

        [Fact]
        public void CompleteScoring_FromScoring_ShouldTransitionToCompleted()
        {
            var answer = new InterviewAnswer();
            answer.StartTranscribing();
            answer.CompleteTranscription("Candidate response text");

            answer.CompleteScoring(8.5m, "Good communication skills");

            answer.Status.Should().Be(AudioProcessingStatus.Completed);
            answer.AiScore.Should().Be(8.5m);
            answer.AiFeedback.Should().Be("Good communication skills");
            answer.CompletedAt.Should().NotBeNull();
            answer.CompletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void CompleteScoring_FromNonScoringState_ShouldThrow()
        {
            var answer = new InterviewAnswer();
            answer.StartTranscribing();

            var act = () => answer.CompleteScoring(8.5m);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot complete scoring from state 'Transcribing'*");
        }

        [Fact]
        public void MarkFailed_ShouldSetStatusAndErrorMessage()
        {
            var answer = new InterviewAnswer();
            answer.StartTranscribing();

            answer.MarkFailed("Whisper API 500 Internal Server Error");

            answer.Status.Should().Be(AudioProcessingStatus.Failed);
            answer.ErrorMessage.Should().Be("Whisper API 500 Internal Server Error");
        }

        [Fact]
        public void RequeueForRetry_ShouldIncrementRetryCountAndResetToPending()
        {
            var answer = new InterviewAnswer();
            answer.StartTranscribing();
            answer.MarkFailed("Transient error");

            answer.RequeueForRetry();

            answer.Status.Should().Be(AudioProcessingStatus.Pending);
            answer.RetryCount.Should().Be(1);
            answer.ProcessingStartedAt.Should().BeNull();
        }

        [Fact]
        public void Interview_InviteToken_ShouldBeInitializedWithValidGuid()
        {
            var interview = new Interview();

            interview.InviteToken.Should().NotBeEmpty();
        }

        [Fact]
        public void UpdateTranscript_ShouldIncrementEditVersionAndSetProperties()
        {
            // Arrange
            var answer = new InterviewAnswer();
            answer.EditVersion.Should().Be(1);

            // Act 1st edit
            answer.UpdateTranscript("First edited transcript", 42);

            // Assert
            answer.EditVersion.Should().Be(2);
            answer.EditedTranscript.Should().Be("First edited transcript");
            answer.LastEditedByUserId.Should().Be(42);
            answer.LastEditedAt.Should().NotBeNull();

            // Act 2nd edit
            answer.UpdateTranscript("Second edited transcript", 99);

            // Assert
            answer.EditVersion.Should().Be(3);
            answer.EditedTranscript.Should().Be("Second edited transcript");
            answer.LastEditedByUserId.Should().Be(99);
        }

        [Fact]
        public void Interview_AssignInterviewer_WhenValid_ShouldSetInterviewerId()
        {
            var interview = new Interview();
            interview.AssignInterviewer(77);

            interview.InterviewerId.Should().Be(77);
        }

        [Fact]
        public void Interview_AssignInterviewer_WhenCancelled_ShouldThrow()
        {
            var interview = new Interview();
            interview.Cancel();

            var act = () => interview.AssignInterviewer(77);
            act.Should().Throw<BusinessRuleException>()
                .WithMessage("*Cannot assign an interviewer to a cancelled interview*");
        }

        [Fact]
        public void ScreeningDecision_Create_WhenOverridingPassWithRejectWithoutReason_ShouldThrow()
        {
            var act = () => ScreeningDecision.Create(
                applicationId: 1,
                interviewId: 1,
                decidedByUserId: 10,
                decidedByRole: "HR",
                decisionType: ScreeningDecisionType.Reject,
                systemRecommendation: "Pass",
                systemOverallScore: 9.0m,
                evaluationSnapshot: new(),
                scoredQuestionCount: 3,
                totalQuestionCount: 3,
                overrideReason: "",
                notes: null);

            act.Should().Throw<BusinessRuleException>()
                .WithMessage("*Bắt buộc phải nhập lý do khi đưa ra quyết định khác với gợi ý sơ bộ*");
        }
    }
}
