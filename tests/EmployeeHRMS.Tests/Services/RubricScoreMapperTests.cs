using System;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class RubricScoreMapperTests
    {
        [Theory]
        [InlineData(RubricLevel.Poor, 2.5)]
        [InlineData(RubricLevel.Acceptable, 5.0)]
        [InlineData(RubricLevel.Good, 7.5)]
        [InlineData(RubricLevel.Excellent, 10.0)]
        public void ToScore_MapsAllRubricLevelsCorrectly(RubricLevel level, decimal expectedScore)
        {
            RubricScoreMapper.ToScore(level).Should().Be(expectedScore);
        }

        [Fact]
        public void ToScore_ThrowsForUndefinedEnum()
        {
            Action act = () => RubricScoreMapper.ToScore((RubricLevel)99);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(0.0, RubricLevel.Poor)]
        [InlineData(2.5, RubricLevel.Poor)]
        [InlineData(3.74, RubricLevel.Poor)]
        [InlineData(3.75, RubricLevel.Acceptable)]
        [InlineData(5.0, RubricLevel.Acceptable)]
        [InlineData(6.24, RubricLevel.Acceptable)]
        [InlineData(6.25, RubricLevel.Good)]
        [InlineData(7.5, RubricLevel.Good)]
        [InlineData(8.74, RubricLevel.Good)]
        [InlineData(8.75, RubricLevel.Excellent)]
        [InlineData(10.0, RubricLevel.Excellent)]
        public void FromScore_MapsNumericalScoresToClosestLevel(decimal score, RubricLevel expectedLevel)
        {
            RubricScoreMapper.FromScore(score).Should().Be(expectedLevel);
        }

        [Theory]
        [InlineData("Excellent", RubricLevel.Excellent)]
        [InlineData("exceptional", RubricLevel.Excellent)]
        [InlineData("Xuất sắc", RubricLevel.Excellent)]
        [InlineData("xuat sac", RubricLevel.Excellent)]
        [InlineData("Level 4", RubricLevel.Excellent)]
        [InlineData("4", RubricLevel.Excellent)]
        [InlineData("Good", RubricLevel.Good)]
        [InlineData("exceeds expectations", RubricLevel.Good)]
        [InlineData("Giỏi", RubricLevel.Good)]
        [InlineData("Khá", RubricLevel.Good)]
        [InlineData("Tốt", RubricLevel.Good)]
        [InlineData("3", RubricLevel.Good)]
        [InlineData("Acceptable", RubricLevel.Acceptable)]
        [InlineData("meets expectations", RubricLevel.Acceptable)]
        [InlineData("Đạt", RubricLevel.Acceptable)]
        [InlineData("Trung bình", RubricLevel.Acceptable)]
        [InlineData("2", RubricLevel.Acceptable)]
        [InlineData("Poor", RubricLevel.Poor)]
        [InlineData("needs improvement", RubricLevel.Poor)]
        [InlineData("Kém", RubricLevel.Poor)]
        [InlineData("Chưa đạt", RubricLevel.Poor)]
        [InlineData("1", RubricLevel.Poor)]
        public void ParseAndTryParse_RecognizesAliasesInEnglishAndVietnamese(string alias, RubricLevel expected)
        {
            RubricScoreMapper.TryParse(alias, out var level).Should().BeTrue();
            level.Should().Be(expected);

            RubricScoreMapper.Parse(alias).Should().Be(expected);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_OnInvalidString()
        {
            Action act = () => RubricScoreMapper.Parse("completely_random_invalid_string");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryParse_ReturnsFalse_OnNullOrWhitespace()
        {
            RubricScoreMapper.TryParse(null, out _).Should().BeFalse();
            RubricScoreMapper.TryParse("   ", out _).Should().BeFalse();
        }

        [Fact]
        public void GetDisplayName_ReturnsDescriptiveNames()
        {
            RubricScoreMapper.GetDisplayName(RubricLevel.Excellent, vietnamese: true).Should().Contain("Xuất sắc");
            RubricScoreMapper.GetDisplayName(RubricLevel.Poor, vietnamese: false).Should().Be("Poor");
        }

        [Fact]
        public void ToScore_WithCustomScoringOptions_UsesCustomScores()
        {
            var customOptions = new ScoringOptions
            {
                PoorScore = 1.0m,
                AcceptableScore = 4.0m,
                GoodScore = 7.0m,
                ExcellentScore = 10.0m
            };

            RubricScoreMapper.ToScore(RubricLevel.Poor, customOptions).Should().Be(1.0m);
            RubricScoreMapper.ToScore(RubricLevel.Acceptable, customOptions).Should().Be(4.0m);
            RubricScoreMapper.ToScore(RubricLevel.Good, customOptions).Should().Be(7.0m);
            RubricScoreMapper.ToScore(RubricLevel.Excellent, customOptions).Should().Be(10.0m);
        }

        [Fact]
        public void FromScore_WithCustomScoringOptions_CalculatesDynamicMidpoints()
        {
            // Custom non-linear scale: Poor = 1.0, Acceptable = 4.0, Good = 7.0, Excellent = 10.0
            // Midpoints: (1+4)/2 = 2.5, (4+7)/2 = 5.5, (7+10)/2 = 8.5
            var customOptions = new ScoringOptions
            {
                PoorScore = 1.0m,
                AcceptableScore = 4.0m,
                GoodScore = 7.0m,
                ExcellentScore = 10.0m
            };

            RubricScoreMapper.FromScore(1.0m, customOptions).Should().Be(RubricLevel.Poor);
            RubricScoreMapper.FromScore(2.49m, customOptions).Should().Be(RubricLevel.Poor);
            RubricScoreMapper.FromScore(2.5m, customOptions).Should().Be(RubricLevel.Acceptable);
            RubricScoreMapper.FromScore(5.49m, customOptions).Should().Be(RubricLevel.Acceptable);
            RubricScoreMapper.FromScore(5.5m, customOptions).Should().Be(RubricLevel.Good);
            RubricScoreMapper.FromScore(8.49m, customOptions).Should().Be(RubricLevel.Good);
            RubricScoreMapper.FromScore(8.5m, customOptions).Should().Be(RubricLevel.Excellent);
            RubricScoreMapper.FromScore(10.0m, customOptions).Should().Be(RubricLevel.Excellent);
        }

        [Fact]
        public void ScoringOptions_Validate_PassesOnValidConfiguration()
        {
            var options = new ScoringOptions();
            Action act = () => options.Validate();
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData(-1.0, 5.0, 7.5, 10.0)] // Negative Poor
        [InlineData(5.0, 5.0, 7.5, 10.0)]  // Poor == Acceptable
        [InlineData(6.0, 5.0, 7.5, 10.0)]  // Poor > Acceptable
        [InlineData(2.5, 8.0, 7.5, 10.0)]  // Acceptable > Good
        [InlineData(2.5, 5.0, 10.5, 10.0)] // Good > Excellent
        [InlineData(2.5, 5.0, 7.5, 10.5)]  // Excellent > 10.0
        public void ScoringOptions_Validate_ThrowsOnInvalidScoreHierarchy(
            decimal poor, decimal acceptable, decimal good, decimal excellent)
        {
            var options = new ScoringOptions
            {
                PoorScore = poor,
                AcceptableScore = acceptable,
                GoodScore = good,
                ExcellentScore = excellent
            };

            Action act = () => options.Validate();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ScoringOptions_Validate_ThrowsWhenPassThresholdLessThanConsiderThreshold()
        {
            var options = new ScoringOptions
            {
                PassThreshold = 4.0m,
                ConsiderThreshold = 5.0m
            };

            Action act = () => options.Validate();
            act.Should().Throw<InvalidOperationException>();
        }
    }
}
