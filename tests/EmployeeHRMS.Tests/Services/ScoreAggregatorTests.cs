using System.Collections.Generic;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class ScoreAggregatorTests
    {
        private readonly ScoringOptions _options = new()
        {
            PassThreshold = 7.0m,
            ConsiderThreshold = 5.0m,
            TechnicalKnockoutMinScore = 5.0m
        };

        [Fact]
        public void Aggregate_WhenEvaluatedCountLessThanTotal_ReturnsIncomplete()
        {
            var evaluated = new List<QuestionEvaluationRecord>
            {
                new() { QuestionId = 1, Outcome = EvaluationOutcome.Succeeded, Score = 7.5m },
                new() { QuestionId = 2, Outcome = EvaluationOutcome.Succeeded, Score = 10.0m }
            };

            var result = ScoreAggregator.Aggregate(evaluated, totalQuestionsCount: 3, _options);

            result.IsFullyEvaluated.Should().BeFalse();
            result.OverallScore.Should().BeNull();
            result.Recommendation.Should().BeNull();
            result.Summary.Should().Contain("2/3");
        }

        [Fact]
        public void Aggregate_WhenAnyQuestionHasFailedOutcome_ReturnsIncomplete()
        {
            var evaluated = new List<QuestionEvaluationRecord>
            {
                new() { QuestionId = 1, Outcome = EvaluationOutcome.Succeeded, Score = 7.5m },
                new() { QuestionId = 2, Outcome = EvaluationOutcome.Failed, Reason = "OpenAI Timeout" }
            };

            var result = ScoreAggregator.Aggregate(evaluated, totalQuestionsCount: 2, _options);

            result.IsFullyEvaluated.Should().BeFalse();
            result.OverallScore.Should().BeNull();
            result.Recommendation.Should().BeNull();
            result.Summary.Should().Contain("lỗi kỹ thuật");
        }

        [Fact]
        public void Aggregate_WhenAnyQuestionIsNotScorable_ReturnsIncompleteWithNotice()
        {
            var evaluated = new List<QuestionEvaluationRecord>
            {
                new() { QuestionId = 1, Outcome = EvaluationOutcome.Succeeded, Score = 7.5m },
                new() { QuestionId = 2, Outcome = EvaluationOutcome.NotScorable, Reason = "Missing rubric" }
            };

            var result = ScoreAggregator.Aggregate(evaluated, totalQuestionsCount: 2, _options);

            result.IsFullyEvaluated.Should().BeFalse();
            result.OverallScore.Should().BeNull();
            result.Recommendation.Should().BeNull();
            result.Summary.Should().Contain("Missing rubric");
        }

        [Fact]
        public void Aggregate_WhenAllQuestionsPassWithHighScores_RecommendsPass()
        {
            var evaluated = new List<QuestionEvaluationRecord>
            {
                new() { QuestionId = 1, Category = QuestionCategory.Technical, Outcome = EvaluationOutcome.Succeeded, Score = 7.5m, Level = RubricLevel.Good },
                new() { QuestionId = 2, Category = QuestionCategory.Technical, Outcome = EvaluationOutcome.Succeeded, Score = 10.0m, Level = RubricLevel.Excellent },
                new() { QuestionId = 3, Category = QuestionCategory.Behavioral, Outcome = EvaluationOutcome.Succeeded, Score = 7.5m, Level = RubricLevel.Good },
                new() { QuestionId = 4, Category = QuestionCategory.Situational, Outcome = EvaluationOutcome.Succeeded, Score = 10.0m, Level = RubricLevel.Excellent }
            };

            var result = ScoreAggregator.Aggregate(evaluated, totalQuestionsCount: 4, _options);

            result.IsFullyEvaluated.Should().BeTrue();
            result.OverallScore.Should().Be(8.75m);
            result.Recommendation.Should().Be(InterviewRecommendation.Pass);
            result.HasTechnicalKnockout.Should().BeFalse();
            result.KnockoutReasons.Should().BeEmpty();
            result.Summary.Should().Contain("PASS");
        }

        [Fact]
        public void Aggregate_WhenAverageIsBorderline_RecommendsConsider()
        {
            var evaluated = new List<QuestionEvaluationRecord>
            {
                new() { QuestionId = 1, Category = QuestionCategory.Technical, Outcome = EvaluationOutcome.Succeeded, Score = 5.0m, Level = RubricLevel.Acceptable },
                new() { QuestionId = 2, Category = QuestionCategory.Behavioral, Outcome = EvaluationOutcome.Succeeded, Score = 5.0m, Level = RubricLevel.Acceptable }
            };

            var result = ScoreAggregator.Aggregate(evaluated, totalQuestionsCount: 2, _options);

            result.IsFullyEvaluated.Should().BeTrue();
            result.OverallScore.Should().Be(5.0m);
            result.Recommendation.Should().Be(InterviewRecommendation.Consider);
            result.HasTechnicalKnockout.Should().BeFalse();
        }

        [Fact]
        public void Aggregate_WhenAverageIsLow_RecommendsReject()
        {
            var evaluated = new List<QuestionEvaluationRecord>
            {
                new() { QuestionId = 1, Category = QuestionCategory.Technical, Outcome = EvaluationOutcome.Succeeded, Score = 2.5m, Level = RubricLevel.Poor },
                new() { QuestionId = 2, Category = QuestionCategory.Behavioral, Outcome = EvaluationOutcome.Succeeded, Score = 5.0m, Level = RubricLevel.Acceptable }
            };

            var result = ScoreAggregator.Aggregate(evaluated, totalQuestionsCount: 2, _options);

            result.IsFullyEvaluated.Should().BeTrue();
            result.OverallScore.Should().Be(3.75m);
            result.Recommendation.Should().Be(InterviewRecommendation.Reject);
        }

        [Fact]
        public void Aggregate_WithCompensatoryScoringTrap_AppliesTechnicalKnockoutAndCapsAtConsider()
        {
            // Candidate scores 10/10 in 3 non-tech questions, but only 2.5/10 in the only Technical question.
            // Raw average = (2.5 + 10 + 10 + 10) / 4 = 8.13 >= 7.0 (Pass)
            // Due to Technical Knockout (2.5 < 5.0), recommendation is capped at Consider!
            var evaluated = new List<QuestionEvaluationRecord>
            {
                new() { QuestionId = 101, Category = QuestionCategory.Technical, Outcome = EvaluationOutcome.Succeeded, Score = 2.5m, Level = RubricLevel.Poor },
                new() { QuestionId = 102, Category = QuestionCategory.Behavioral, Outcome = EvaluationOutcome.Succeeded, Score = 10.0m, Level = RubricLevel.Excellent },
                new() { QuestionId = 103, Category = QuestionCategory.Situational, Outcome = EvaluationOutcome.Succeeded, Score = 10.0m, Level = RubricLevel.Excellent },
                new() { QuestionId = 104, Category = QuestionCategory.CulturalFit, Outcome = EvaluationOutcome.Succeeded, Score = 10.0m, Level = RubricLevel.Excellent }
            };

            var result = ScoreAggregator.Aggregate(evaluated, totalQuestionsCount: 4, _options);

            result.IsFullyEvaluated.Should().BeTrue();
            result.OverallScore.Should().Be(8.13m);
            result.HasTechnicalKnockout.Should().BeTrue();
            result.KnockoutReasons.Should().ContainSingle(r => r.Contains("#101") && r.Contains("2.5"));
            result.Recommendation.Should().Be(InterviewRecommendation.Consider);
            result.Summary.Should().Contain("hạ xuống Consider do không đạt yêu cầu ở câu hỏi kỹ thuật cốt lõi");
        }

        [Fact]
        public void Aggregate_WhenCandidateRemainsSilent_EvaluatesAsZeroScoreAndTriggersKnockoutIfTechnical()
        {
            var evaluated = new List<QuestionEvaluationRecord>
            {
                new() { QuestionId = 1, Category = QuestionCategory.Technical, Outcome = EvaluationOutcome.NoResponse, Reason = "Silence" },
                new() { QuestionId = 2, Category = QuestionCategory.Behavioral, Outcome = EvaluationOutcome.Succeeded, Score = 10.0m, Level = RubricLevel.Excellent }
            };

            var result = ScoreAggregator.Aggregate(evaluated, totalQuestionsCount: 2, _options);

            result.IsFullyEvaluated.Should().BeTrue();
            result.OverallScore.Should().Be(5.0m); // (0 + 10) / 2
            result.HasTechnicalKnockout.Should().BeTrue();
            result.KnockoutReasons.Should().ContainSingle(r => r.Contains("#1") && r.Contains("0.0"));
            result.Recommendation.Should().Be(InterviewRecommendation.Consider);
        }
    }
}
