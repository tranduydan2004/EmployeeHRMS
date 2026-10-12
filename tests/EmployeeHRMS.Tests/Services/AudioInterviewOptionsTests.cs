using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Models;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class AudioInterviewOptionsTests
    {
        private readonly AudioInterviewOptions _options = new();

        [Theory]
        [InlineData(JobLevel.Intern, 20, 60)]
        [InlineData(JobLevel.Fresher, 20, 60)]
        [InlineData(JobLevel.Junior, 20, 60)]
        [InlineData(JobLevel.Middle, 20, 60)]
        [InlineData(JobLevel.Senior, 30, 90)]
        [InlineData(JobLevel.Lead, 30, 90)]
        public void SuggestTimeLimit_TechnicalQuestions_ReturnsExpectedTimeLimit(
            JobLevel difficulty,
            int expectedThink,
            int expectedAnswer)
        {
            var (think, answer) = _options.SuggestTimeLimit(QuestionCategory.Technical, difficulty);

            think.Should().Be(expectedThink);
            answer.Should().Be(expectedAnswer);
        }

        [Theory]
        [InlineData(JobLevel.Intern)]
        [InlineData(JobLevel.Junior)]
        [InlineData(JobLevel.Senior)]
        [InlineData(JobLevel.Lead)]
        public void SuggestTimeLimit_SituationalQuestions_Returns20And90(JobLevel difficulty)
        {
            var (think, answer) = _options.SuggestTimeLimit(QuestionCategory.Situational, difficulty);

            think.Should().Be(20);
            answer.Should().Be(90);
        }

        [Theory]
        [InlineData(JobLevel.Intern)]
        [InlineData(JobLevel.Junior)]
        [InlineData(JobLevel.Senior)]
        public void SuggestTimeLimit_BehavioralQuestions_Returns15And60(JobLevel difficulty)
        {
            var (think, answer) = _options.SuggestTimeLimit(QuestionCategory.Behavioral, difficulty);

            think.Should().Be(15);
            answer.Should().Be(60);
        }

        [Theory]
        [InlineData(JobLevel.Intern)]
        [InlineData(JobLevel.Junior)]
        [InlineData(JobLevel.Senior)]
        public void SuggestTimeLimit_CulturalFitQuestions_Returns15And45(JobLevel difficulty)
        {
            var (think, answer) = _options.SuggestTimeLimit(QuestionCategory.CulturalFit, difficulty);

            think.Should().Be(15);
            answer.Should().Be(45);
        }

        [Fact]
        public void SuggestTimeLimit_WhenPresetsExceedBounds_ClampsToMinMax()
        {
            var customOptions = new AudioInterviewOptions
            {
                MinThinkSeconds = 10,
                MaxThinkSeconds = 25,
                MinAnswerSeconds = 30,
                MaxAnswerSeconds = 80,
                CulturalFitThinkSeconds = 5,  // Below min (10)
                TechnicalSeniorAnswerSeconds = 100 // Above max (80)
            };

            var (thinkLow, _) = customOptions.SuggestTimeLimit(QuestionCategory.CulturalFit, JobLevel.Junior);
            thinkLow.Should().Be(10); // Clamped to MinThinkSeconds

            var (_, answerHigh) = customOptions.SuggestTimeLimit(QuestionCategory.Technical, JobLevel.Senior);
            answerHigh.Should().Be(80); // Clamped to MaxAnswerSeconds
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(5, true)]
        [InlineData(20, true)]
        [InlineData(40, true)]
        [InlineData(4, false)]
        [InlineData(41, false)]
        [InlineData(-1, false)]
        public void IsThinkSecondsValid_ValidatesCorrectly(int? seconds, bool expected)
        {
            _options.IsThinkSecondsValid(seconds).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(10, true)]
        [InlineData(60, true)]
        [InlineData(120, true)]
        [InlineData(9, false)]
        [InlineData(121, false)]
        [InlineData(0, false)]
        public void IsAnswerSecondsValid_ValidatesCorrectly(int? seconds, bool expected)
        {
            _options.IsAnswerSecondsValid(seconds).Should().Be(expected);
        }
    }
}
