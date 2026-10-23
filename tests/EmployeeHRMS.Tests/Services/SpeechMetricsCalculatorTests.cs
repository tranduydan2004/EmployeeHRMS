using System.Collections.Generic;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class SpeechMetricsCalculatorTests
    {
        private readonly SpeechMetricsOptions _options = new()
        {
            PauseVariationCvThreshold = 0.15,
            KnownHallucinationPhrases = new List<string>
            {
                "Cảm ơn các bạn đã theo dõi",
                "Thank you for watching",
                "Hẹn gặp lại"
            },
            MaxNoSpeechProbability = 0.8
        };

        [Fact]
        public void Calculate_WithNullOrEmptyWords_ReturnsZeroMetrics()
        {
            var result = SpeechMetricsCalculator.Calculate(null, 60, _options);

            result.Wpm.Should().Be(0);
            result.SpeechDurationSeconds.Should().Be(0);
            result.WordCount.Should().Be(0);
            result.PauseVariationCv.Should().Be(0);
            result.AveragePauseSeconds.Should().Be(0);
            result.PauseCount.Should().Be(0);
            result.IsRhythmMonotonous.Should().BeFalse();
        }

        [Fact]
        public void Calculate_WithSingleWord_CalculatesSafelyWithoutDivisionByZero()
        {
            var words = new List<WordTimestamp>
            {
                new() { Word = "Xin", Start = 1.0, End = 1.5 }
            };

            var result = SpeechMetricsCalculator.Calculate(words, 10.0, _options);

            result.WordCount.Should().Be(1);
            result.PauseCount.Should().Be(0);
            result.PauseVariationCv.Should().Be(0);
            result.IsRhythmMonotonous.Should().BeFalse();
        }

        [Fact]
        public void Calculate_WithNaturalSpeech_CalculatesWpmAndCv()
        {
            // 6 words with variable pauses: 0.2s, 0.8s, 0.1s, 0.6s, 0.3s
            var words = new List<WordTimestamp>
            {
                new() { Word = "Tôi", Start = 0.0, End = 0.5 },
                new() { Word = "có", Start = 0.7, End = 1.0 }, // pause = 0.2
                new() { Word = "nhiều", Start = 1.8, End = 2.2 }, // pause = 0.8
                new() { Word = "năm", Start = 2.3, End = 2.6 }, // pause = 0.1
                new() { Word = "kinh", Start = 3.2, End = 3.6 }, // pause = 0.6
                new() { Word = "nghiệm", Start = 3.9, End = 4.4 } // pause = 0.3
            };

            var result = SpeechMetricsCalculator.Calculate(words, 5.0, _options);

            result.WordCount.Should().Be(6);
            result.PauseCount.Should().Be(5);
            result.AveragePauseSeconds.Should().BeGreaterThan(0);
            result.PauseVariationCv.Should().BeGreaterThan(0.2); // high variation
            result.IsRhythmMonotonous.Should().BeFalse();
        }

        [Fact]
        public void Calculate_WithMonotonousRhythm_FlagsIsRhythmMonotonous()
        {
            // 7 words with robotic identical pauses of 0.20s each
            var words = new List<WordTimestamp>
            {
                new() { Word = "word1", Start = 0.0, End = 0.5 },
                new() { Word = "word2", Start = 0.7, End = 1.2 }, // pause = 0.20
                new() { Word = "word3", Start = 1.4, End = 1.9 }, // pause = 0.20
                new() { Word = "word4", Start = 2.1, End = 2.6 }, // pause = 0.20
                new() { Word = "word5", Start = 2.8, End = 3.3 }, // pause = 0.20
                new() { Word = "word6", Start = 3.5, End = 4.0 }, // pause = 0.20
                new() { Word = "word7", Start = 4.2, End = 4.7 }  // pause = 0.20
            };

            var result = SpeechMetricsCalculator.Calculate(words, 5.0, _options);

            result.WordCount.Should().Be(7);
            result.PauseCount.Should().Be(6);
            result.PauseVariationCv.Should().BeLessThan(0.05); // near zero variation
            result.IsRhythmMonotonous.Should().BeTrue();
        }

        [Fact]
        public void ParseSegmentsJson_WithValidWhisperVerboseJson_ExtractsWordsAndSegments()
        {
            string json = """
            {
                "words": [
                    { "word": "Hello", "start": 0.5, "end": 0.9 },
                    { "word": "world", "start": 1.1, "end": 1.5 }
                ],
                "segments": [
                    { "id": 0, "start": 0.5, "end": 1.5, "text": "Hello world", "noSpeechProbability": 0.02 }
                ]
            }
            """;

            var (words, segments) = SpeechMetricsCalculator.ParseSegmentsJson(json);

            words.Should().HaveCount(2);
            words[0].Word.Should().Be("Hello");
            words[0].Start.Should().Be(0.5);

            segments.Should().HaveCount(1);
            segments[0].Text.Should().Be("Hello world");
            segments[0].NoSpeechProbability.Should().Be(0.02);
        }

        [Fact]
        public void ParseSegmentsJson_WithCorruptedJson_ReturnsEmptyListsSafely()
        {
            var (words, segments) = SpeechMetricsCalculator.ParseSegmentsJson("invalid json {{");

            words.Should().BeEmpty();
            segments.Should().BeEmpty();
        }

        [Theory]
        [InlineData("Cảm ơn các bạn đã theo dõi.", true)]
        [InlineData("THANK YOU FOR WATCHING!", true)]
        [InlineData("Cảm ơn các bạn đã theo dõi hẹn gặp lại", true)]
        [InlineData("Tôi là kỹ sư phần mềm backend với 3 năm kinh nghiệm .NET", false)]
        public void IsKnownHallucination_DetectsRepetitiveSilencePhrases(string transcript, bool expected)
        {
            bool isHallucination = SpeechMetricsCalculator.IsKnownHallucination(transcript, _options);

            isHallucination.Should().Be(expected);
        }

        [Fact]
        public void IsSilentSegments_DetectsSegmentsAboveSilenceThreshold()
        {
            var silentSegments = new List<SegmentData>
            {
                new() { Id = 0, NoSpeechProbability = 0.95 },
                new() { Id = 1, NoSpeechProbability = 0.88 }
            };

            var normalSegments = new List<SegmentData>
            {
                new() { Id = 0, NoSpeechProbability = 0.12 },
                new() { Id = 1, NoSpeechProbability = 0.05 }
            };

            SpeechMetricsCalculator.IsSilentSegments(silentSegments, _options).Should().BeTrue();
            SpeechMetricsCalculator.IsSilentSegments(normalSegments, _options).Should().BeFalse();
        }
    }
}
