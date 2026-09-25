using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;
using EmployeeHRMS.Api.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Implementation gọi OpenAI API sinh bản thảo JD theo schema có cấu trúc.
    /// Sử dụng Structured Outputs (strict JSON schema) + corrective retry.
    /// </summary>
    public class OpenAiJdGenerationService : IJdGenerationService
    {
        private readonly ChatClient _chatClient;
        private readonly OpenAiSettings _settings;
        private readonly ILogger<OpenAiJdGenerationService> _logger;

        // JSON Schema cho JD output — strict mode, mọi field required, không additionalProperties
        private static readonly BinaryData JdJsonSchema = BinaryData.FromString("""
        {
            "type": "object",
            "properties": {
                "intro": {
                    "type": "string",
                    "description": "1-2 đoạn giới thiệu về vị trí và môi trường làm việc, tối đa 200 từ"
                },
                "responsibilities": {
                    "type": "array",
                    "items": { "type": "string" },
                    "description": "5-8 trách nhiệm chính, mỗi item là 1 câu hoàn chỉnh"
                },
                "mustHave": {
                    "type": "array",
                    "items": { "type": "string" },
                    "description": "4-6 yêu cầu bắt buộc (kỹ năng, kinh nghiệm, bằng cấp)"
                },
                "niceToHave": {
                    "type": "array",
                    "items": { "type": "string" },
                    "description": "2-4 yêu cầu ưu tiên nhưng không bắt buộc"
                },
                "benefits": {
                    "type": "array",
                    "items": { "type": "string" },
                    "description": "4-6 quyền lợi và phúc lợi"
                }
            },
            "required": ["intro", "responsibilities", "mustHave", "niceToHave", "benefits"],
            "additionalProperties": false
        }
        """);

        private const string SystemPrompt = """
            You are a professional HR Job Description writer for a corporate HRMS system.
            Your task is to generate structured, professional job descriptions in English.

            RULES:
            - Write in a professional, engaging tone suitable for job boards
            - Be specific and actionable — avoid vague phrases like "various tasks"
            - Do NOT invent company-specific information (company name, culture, perks) unless explicitly provided
            - If salary range is provided, mention competitive compensation in benefits but do NOT state exact figures in the JD
            - Tailor responsibilities and requirements to match the specified level (Intern vs Senior vs Lead)
            - Each array item should be a complete, standalone sentence or phrase
            """;

        // Few-shot example giúp model bám đúng văn phong và cấu trúc
        private const string FewShotUserExample = """
            Generate a JD for:
            - Title: Backend Developer
            - Department: Engineering
            - Level: Junior
            - Work Mode: Hybrid
            - Core Skills: C#, ASP.NET Core, SQL Server
            - Years of Experience: 1
            - Certifications: (none)
            """;

        private const string FewShotAssistantExample = """
            {
                "intro": "We are looking for a motivated Junior Backend Developer to join our Engineering team. In this hybrid role, you will work closely with senior engineers to design, develop, and maintain backend services using modern .NET technologies. This is an excellent opportunity for early-career developers eager to grow their skills in a collaborative environment.",
                "responsibilities": [
                    "Develop and maintain RESTful APIs using ASP.NET Core under senior guidance",
                    "Write clean, testable code following team coding standards and best practices",
                    "Participate in code reviews to learn and improve code quality",
                    "Collaborate with frontend developers to integrate APIs with user-facing features",
                    "Assist in database design and write efficient SQL queries",
                    "Contribute to technical documentation and knowledge sharing sessions"
                ],
                "mustHave": [
                    "At least 1 year of professional experience or strong internship background with C# and .NET",
                    "Basic understanding of RESTful API design principles",
                    "Familiarity with SQL Server or similar relational databases",
                    "Strong willingness to learn and adapt to new technologies"
                ],
                "niceToHave": [
                    "Experience with Entity Framework Core or Dapper",
                    "Familiarity with Git version control and CI/CD pipelines",
                    "Exposure to unit testing frameworks (xUnit, NUnit)"
                ],
                "benefits": [
                    "Competitive salary with annual performance reviews",
                    "Flexible hybrid work arrangement (3 days office, 2 days remote)",
                    "Structured mentorship program with dedicated senior buddy",
                    "Annual training budget for courses and certifications",
                    "Comprehensive health insurance coverage"
                ]
            }
            """;

        public OpenAiJdGenerationService(
            ChatClient chatClient,
            IOptions<OpenAiSettings> settings,
            ILogger<OpenAiJdGenerationService> logger)
        {
            _chatClient = chatClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<JdContent> GenerateJdAsync(JdGenerationPromptData promptData, CancellationToken ct = default)
        {
            var userPrompt = BuildUserPrompt(promptData);

            var options = new ChatCompletionOptions
            {
                Temperature = _settings.JdTemperature,
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "job_description",
                    jsonSchema: JdJsonSchema,
                    jsonSchemaIsStrict: true)
            };

            // Retry loop: nếu parse thất bại, gọi lại kèm thông báo lỗi
            string? lastError = null;
            for (int attempt = 0; attempt <= _settings.MaxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(SystemPrompt),
                    // Few-shot example
                    new UserChatMessage(FewShotUserExample),
                    new AssistantChatMessage(FewShotAssistantExample),
                    // Actual request
                    new UserChatMessage(userPrompt)
                };

                // Nếu đây là retry, thêm thông báo lỗi để LLM sửa
                if (lastError != null)
                {
                    messages.Add(new UserChatMessage(
                        $"Your previous response had an error: {lastError}. Please generate a valid response following the exact JSON schema."));
                }

                _logger.LogInformation(
                    "Calling OpenAI for JD generation (attempt {Attempt}/{MaxRetries}) — Title: {Title}",
                    attempt + 1, _settings.MaxRetries + 1, promptData.JobTitle);

                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, ct);
                var responseText = completion.Content[0].Text;

                // TryParse pattern: không để app crash nếu JSON sai format
                if (TryParseJdContent(responseText, out var jdContent, out var parseError))
                {
                    _logger.LogInformation("JD generated successfully for: {Title}", promptData.JobTitle);
                    return jdContent!;
                }

                lastError = parseError;
                _logger.LogWarning(
                    "JD parse failed (attempt {Attempt}/{MaxRetries}): {Error}",
                    attempt + 1, _settings.MaxRetries + 1, parseError);
            }

            // Sau tất cả retries vẫn thất bại — trả lỗi rõ ràng cho HR
            throw new InvalidOperationException(
                $"Failed to generate valid JD after {_settings.MaxRetries + 1} attempts. Last error: {lastError}");
        }

        private static string BuildUserPrompt(JdGenerationPromptData data)
        {
            var lines = new List<string>
            {
                "Generate a job description for:",
                $"- Title: {data.JobTitle}",
                $"- Department: {data.DepartmentName}",
                $"- Level: {data.Level}",
                $"- Work Mode: {data.WorkMode}"
            };

            if (data.CoreSkills.Count > 0)
                lines.Add($"- Core Skills: {string.Join(", ", data.CoreSkills)}");

            if (data.YearsOfExperience.HasValue)
                lines.Add($"- Years of Experience: {data.YearsOfExperience}");

            if (data.SalaryMin.HasValue || data.SalaryMax.HasValue)
            {
                var salaryParts = new List<string>();
                if (data.SalaryMin.HasValue) salaryParts.Add($"Min: {data.SalaryMin:N0}");
                if (data.SalaryMax.HasValue) salaryParts.Add($"Max: {data.SalaryMax:N0}");
                salaryParts.Add($"Currency: {data.Currency}");
                lines.Add($"- Salary Range: {string.Join(", ", salaryParts)}");
            }

            if (data.Certifications.Count > 0)
                lines.Add($"- Preferred Certifications: {string.Join(", ", data.Certifications)}");

            if (!string.IsNullOrWhiteSpace(data.AdditionalNotes))
                lines.Add($"- Additional Notes: {data.AdditionalNotes}");

            return string.Join("\n", lines);
        }

        /// <summary>
        /// TryParse pattern: cố gắng deserialize JSON, trả false + lỗi cụ thể nếu thất bại.
        /// Validate semantic: các array không được rỗng, intro không được blank.
        /// </summary>
        private static bool TryParseJdContent(string json, out JdContent? result, out string? error)
        {
            result = null;
            error = null;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                result = JsonSerializer.Deserialize<JdContent>(json, options);

                if (result == null)
                {
                    error = "Deserialized result is null";
                    return false;
                }

                // Semantic validation
                if (string.IsNullOrWhiteSpace(result.Intro))
                {
                    error = "Intro field is empty";
                    return false;
                }

                if (result.Responsibilities.Count == 0)
                {
                    error = "Responsibilities array is empty";
                    return false;
                }

                if (result.MustHave.Count == 0)
                {
                    error = "MustHave array is empty";
                    return false;
                }

                return true;
            }
            catch (JsonException ex)
            {
                error = $"JSON parse error: {ex.Message}";
                return false;
            }
        }
    }
}
