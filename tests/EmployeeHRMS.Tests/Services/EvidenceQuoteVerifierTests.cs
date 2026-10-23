using System.Collections.Generic;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class EvidenceQuoteVerifierTests
    {
        private const string SampleTranscript =
            "Tôi đã có 3 năm kinh nghiệm làm việc với ASP.NET Core và PostgreSQL. " +
            "Trong dự án trước, tôi đã tối ưu hóa câu truy vấn EF Core bằng cách sử dụng AsNoTracking " +
            "và thêm index thích hợp, giúp giảm thời gian phản hồi từ 1200ms xuống còn 150ms.";

        [Fact]
        public void Verify_WithNullOrEmptyQuotes_ReturnsValidWithOneRatio()
        {
            var result = EvidenceQuoteVerifier.Verify(null, SampleTranscript);

            result.IsValid.Should().BeTrue();
            result.TotalQuotes.Should().Be(0);
            result.VerificationRatio.Should().Be(1.0);
            result.VerifiedQuotes.Should().BeEmpty();
            result.UnverifiedQuotes.Should().BeEmpty();
        }

        [Fact]
        public void Verify_WithQuotesAndEmptyTranscript_FlagsAllAsUnverified()
        {
            var quotes = new List<string> { "ASP.NET Core", "PostgreSQL" };

            var result = EvidenceQuoteVerifier.Verify(quotes, null);

            result.IsValid.Should().BeFalse();
            result.TotalQuotes.Should().Be(2);
            result.VerifiedQuotesCount.Should().Be(0);
            result.UnverifiedQuotesCount.Should().Be(2);
            result.VerificationRatio.Should().Be(0.0);
        }

        [Fact]
        public void Verify_WithExactQuotes_VerifiesSuccessfully()
        {
            var quotes = new List<string>
            {
                "3 năm kinh nghiệm làm việc",
                "tối ưu hóa câu truy vấn EF Core"
            };

            var result = EvidenceQuoteVerifier.Verify(quotes, SampleTranscript);

            result.IsValid.Should().BeTrue();
            result.TotalQuotes.Should().Be(2);
            result.VerifiedQuotesCount.Should().Be(2);
            result.VerificationRatio.Should().Be(1.0);
            result.VerifiedQuotes.Should().Contain(quotes);
        }

        [Fact]
        public void Verify_WithNormalizedVariations_VerifiesSuccessfully()
        {
            // Quote has different casing, extra spaces, and lacks punctuation present in transcript
            var quotes = new List<string>
            {
                "3 NĂM   KINH NGHIỆM",
                "thời gian phản hồi từ 1200ms xuống còn 150ms"
            };

            var result = EvidenceQuoteVerifier.Verify(quotes, SampleTranscript);

            result.IsValid.Should().BeTrue();
            result.VerifiedQuotesCount.Should().Be(2);
            result.VerificationRatio.Should().Be(1.0);
        }

        [Fact]
        public void Verify_WithHallucinatedQuotes_DetectsUnverifiedItems()
        {
            var quotes = new List<string>
            {
                "3 năm kinh nghiệm làm việc", // exists
                "Tôi đã xây dựng hệ thống Kafka và Redis Cluster", // hallucinated
                "giảm thời gian phản hồi từ 1200ms xuống còn 150ms" // exists
            };

            var result = EvidenceQuoteVerifier.Verify(quotes, SampleTranscript);

            result.IsValid.Should().BeFalse();
            result.TotalQuotes.Should().Be(3);
            result.VerifiedQuotesCount.Should().Be(2);
            result.UnverifiedQuotesCount.Should().Be(1);
            result.UnverifiedQuotes.Should().Contain("Tôi đã xây dựng hệ thống Kafka và Redis Cluster");
            result.VerificationRatio.Should().BeApproximately(0.67, 0.02);
        }
    }
}
