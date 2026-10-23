using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using EmployeeHRMS.Api.DTOs;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.DTOs
{
    /// <summary>
    /// Kiểm thử bảo mật (Data Leak Prevention) cho toàn bộ DTO của các endpoint ẩn danh dành cho ứng viên.
    /// Khẳng định tuyệt đối KHÔNG chứa hoặc serialize ra:
    /// AiScore, AiFeedback, Transcript, ErrorMessage, Flags, Rubric, hay AudioStorageKey.
    /// </summary>
    public class CandidateDtoSecurityTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private static readonly string[] ForbiddenFieldFragments = new[]
        {
            "aiscore",
            "aifeedback",
            "transcript",
            "errormessage",
            "rubric",
            "flags",
            "audiostoragekey",
            "rawtranscript",
            "editedtranscript"
        };

        [Fact]
        public void CandidateAudioAnswerResponseDto_Serialization_ContainsOnlyQuestionIdAndReceived()
        {
            // Arrange
            var dto = new CandidateAudioAnswerResponseDto
            {
                QuestionId = 42,
                Received = true
            };

            // Act
            var json = JsonSerializer.Serialize(dto, JsonOptions);

            // Assert
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Chỉ có đúng 2 thuộc tính: questionId và received
            var propertyCount = 0;
            foreach (var prop in root.EnumerateObject())
            {
                propertyCount++;
                prop.Name.Should().BeOneOf("questionId", "received");
            }
            propertyCount.Should().Be(2);

            root.GetProperty("questionId").GetInt32().Should().Be(42);
            root.GetProperty("received").GetBoolean().Should().BeTrue();

            // Khẳng định không chứa bất kỳ từ khóa cấm nào
            foreach (var forbidden in ForbiddenFieldFragments)
            {
                json.ToLowerInvariant().Should().NotContain(forbidden,
                    because: $"CandidateAudioAnswerResponseDto không được phép để lộ thông tin '{forbidden}' cho ứng viên.");
            }
        }

        [Fact]
        public void CandidateInterviewSessionResponseDto_Serialization_NeverExposesSensitiveOrInternalFields()
        {
            // Arrange
            var sessionDto = new CandidateInterviewSessionResponseDto
            {
                InviteToken = Guid.NewGuid(),
                JobTitle = "Senior Backend Engineer",
                CandidateName = "Nguyen Van A",
                Status = "InProgress",
                InviteExpiresAt = DateTime.UtcNow.AddDays(2),
                ConsentedAt = DateTime.UtcNow,
                TotalQuestions = 1,
                Questions = new List<CandidateQuestionDto>
                {
                    new CandidateQuestionDto
                    {
                        Id = 101,
                        Question = "Hãy mô tả kinh nghiệm thiết kế API bảo mật của bạn?",
                        OrderIndex = 1,
                        ThinkSeconds = 30,
                        AnswerSeconds = 120,
                        HasAnswer = true
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(sessionDto, JsonOptions);

            // Assert
            foreach (var forbidden in ForbiddenFieldFragments)
            {
                json.ToLowerInvariant().Should().NotContain(forbidden,
                    because: $"CandidateInterviewSessionResponseDto không được phép chứa hoặc để lộ '{forbidden}'.");
            }
        }

        [Fact]
        public void CandidateInterviewStartResponseDto_Serialization_NeverExposesSensitiveFields()
        {
            // Arrange
            var startDto = new CandidateInterviewStartResponseDto
            {
                InviteToken = Guid.NewGuid(),
                Status = "InProgress",
                ConsentedAt = DateTime.UtcNow,
                Message = "Buổi phỏng vấn đã bắt đầu."
            };

            // Act
            var json = JsonSerializer.Serialize(startDto, JsonOptions);

            // Assert
            foreach (var forbidden in ForbiddenFieldFragments)
            {
                json.ToLowerInvariant().Should().NotContain(forbidden,
                    because: $"CandidateInterviewStartResponseDto không được phép chứa hoặc để lộ '{forbidden}'.");
            }
        }

        [Theory]
        [InlineData(typeof(CandidateAudioAnswerResponseDto))]
        [InlineData(typeof(CandidateInterviewSessionResponseDto))]
        [InlineData(typeof(CandidateQuestionDto))]
        [InlineData(typeof(CandidateInterviewStartResponseDto))]
        public void CandidateAnonymousDtos_Reflection_MustNotDeclareSensitiveProperties(Type dtoType)
        {
            // Arrange & Act
            var properties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Assert
            foreach (var prop in properties)
            {
                var lowerName = prop.Name.ToLowerInvariant();
                foreach (var forbidden in ForbiddenFieldFragments)
                {
                    lowerName.Should().NotContain(forbidden,
                        because: $"Type '{dtoType.Name}' không được khai báo property '{prop.Name}' vi phạm chính sách bảo mật ứng viên.");
                }
            }
        }
    }
}
