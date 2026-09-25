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
    /// Implementation gọi OpenAI API sinh bộ câu hỏi phỏng vấn tình huống
    /// kèm barem chấm điểm ScoringRubric 4 mức.
    /// </summary>
    public class OpenAiQuestionBankService : IQuestionBankService
    {
        private readonly ChatClient _chatClient;
        private readonly OpenAiSettings _settings;
        private readonly ILogger<OpenAiQuestionBankService> _logger;

        // JSON Schema cho Question Bank output — mảng object lồng nhau, strict mode
        private static readonly BinaryData QuestionBankJsonSchema = BinaryData.FromString("""
        {
            "type": "object",
            "properties": {
                "questions": {
                    "type": "array",
                    "description": "3-5 câu hỏi phỏng vấn tình huống",
                    "items": {
                        "type": "object",
                        "properties": {
                            "question": {
                                "type": "string",
                                "description": "Câu hỏi phỏng vấn hoàn chỉnh, rõ ràng"
                            },
                            "category": {
                                "type": "string",
                                "enum": ["Technical", "Behavioral", "Situational", "CulturalFit"],
                                "description": "Phân loại câu hỏi"
                            },
                            "difficulty": {
                                "type": "string",
                                "enum": ["Intern", "Fresher", "Junior", "Middle", "Senior", "Lead"],
                                "description": "Mức độ khó phù hợp với level ứng viên"
                            },
                            "scoringRubric": {
                                "type": "object",
                                "properties": {
                                    "excellent": {
                                        "type": "string",
                                        "description": "Mô tả câu trả lời xuất sắc (9-10 điểm)"
                                    },
                                    "good": {
                                        "type": "string",
                                        "description": "Mô tả câu trả lời tốt (7-8 điểm)"
                                    },
                                    "acceptable": {
                                        "type": "string",
                                        "description": "Mô tả câu trả lời chấp nhận được (5-6 điểm)"
                                    },
                                    "poor": {
                                        "type": "string",
                                        "description": "Mô tả câu trả lời yếu (dưới 5 điểm)"
                                    }
                                },
                                "required": ["excellent", "good", "acceptable", "poor"],
                                "additionalProperties": false
                            }
                        },
                        "required": ["question", "category", "difficulty", "scoringRubric"],
                        "additionalProperties": false
                    }
                }
            },
            "required": ["questions"],
            "additionalProperties": false
        }
        """);

        private const string SystemPrompt = """
            You are a senior HR interview specialist for a corporate HRMS system.
            Your task is to generate situational interview questions with detailed scoring rubrics.

            RULES:
            - Generate 3-5 questions that directly assess the skills and responsibilities described in the JD
            - Mix question categories: include at least 1 Technical and 1 Behavioral/Situational question
            - Match question difficulty to the specified level (e.g., Junior questions should not require architect-level knowledge)
            - Each scoring rubric level must describe specific, observable behaviors — not vague qualities
            - Excellent: demonstrates mastery and depth beyond the requirement
            - Good: meets the requirement with solid understanding
            - Acceptable: shows basic awareness but lacks depth
            - Poor: demonstrates fundamental misunderstanding or no relevant experience
            - Write all questions and rubrics in English
            """;

        private const string FewShotUserExample = """
            Generate interview questions for:
            - Title: Backend Developer
            - Level: Junior
            - JD Intro: We are looking for a Junior Backend Developer to join our Engineering team...
            - Key Responsibilities: Develop RESTful APIs, Write SQL queries, Participate in code reviews
            - Must-Have Skills: C#, ASP.NET Core, SQL Server
            """;

        private const string FewShotAssistantExample = """
            {
                "questions": [
                    {
                        "question": "Describe a time when you had to debug a complex issue in a web API. Walk us through your approach from identifying the problem to implementing the fix.",
                        "category": "Technical",
                        "difficulty": "Junior",
                        "scoringRubric": {
                            "excellent": "Demonstrates a systematic debugging approach: reproducing the issue, using logging/breakpoints, isolating the root cause, implementing and testing the fix, and adding preventive measures like unit tests or monitoring.",
                            "good": "Describes a logical debugging process with clear steps: reproducing, using basic debugging tools, fixing the issue, and verifying the solution works.",
                            "acceptable": "Can describe a debugging scenario but the approach is somewhat ad-hoc — relies mainly on trial and error or console logging without a structured methodology.",
                            "poor": "Cannot articulate a clear debugging process, or describes simply asking a colleague for help without attempting to understand the problem independently."
                        }
                    }
                ]
            }
            """;

        public OpenAiQuestionBankService(
            ChatClient chatClient,
            IOptions<OpenAiSettings> settings,
            ILogger<OpenAiQuestionBankService> logger)
        {
            _chatClient = chatClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<List<QuestionBankGenerationResult>> GenerateQuestionsAsync(
            QuestionBankPromptData promptData, CancellationToken ct = default)
        {
            var userPrompt = BuildUserPrompt(promptData);

            var options = new ChatCompletionOptions
            {
                Temperature = _settings.QuestionTemperature,
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "question_bank",
                    jsonSchema: QuestionBankJsonSchema,
                    jsonSchemaIsStrict: true)
            };

            string? lastError = null;
            for (int attempt = 0; attempt <= _settings.MaxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(SystemPrompt),
                    new UserChatMessage(FewShotUserExample),
                    new AssistantChatMessage(FewShotAssistantExample),
                    new UserChatMessage(userPrompt)
                };

                if (lastError != null)
                {
                    messages.Add(new UserChatMessage(
                        $"Your previous response had an error: {lastError}. Please generate a valid response following the exact JSON schema."));
                }

                _logger.LogInformation(
                    "Calling OpenAI for Question Bank generation (attempt {Attempt}/{MaxRetries}) — Title: {Title}",
                    attempt + 1, _settings.MaxRetries + 1, promptData.JobTitle);

                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, ct);
                var responseText = completion.Content[0].Text;

                if (TryParseQuestions(responseText, out var questions, out var parseError))
                {
                    _logger.LogInformation(
                        "Question Bank generated successfully: {Count} questions for {Title}",
                        questions!.Count, promptData.JobTitle);
                    return questions;
                }

                lastError = parseError;
                _logger.LogWarning(
                    "Question Bank parse failed (attempt {Attempt}/{MaxRetries}): {Error}",
                    attempt + 1, _settings.MaxRetries + 1, parseError);
            }

            throw new InvalidOperationException(
                $"Failed to generate valid Question Bank after {_settings.MaxRetries + 1} attempts. Last error: {lastError}");
        }

        private static string BuildUserPrompt(QuestionBankPromptData data)
        {
            var lines = new List<string>
            {
                "Generate interview questions for:",
                $"- Title: {data.JobTitle}",
                $"- Level: {data.Level}",
                $"- JD Intro: {data.JdContent.Intro}"
            };

            if (data.JdContent.Responsibilities.Count > 0)
                lines.Add($"- Key Responsibilities: {string.Join("; ", data.JdContent.Responsibilities)}");

            if (data.JdContent.MustHave.Count > 0)
                lines.Add($"- Must-Have Skills: {string.Join("; ", data.JdContent.MustHave)}");

            if (data.JdContent.NiceToHave.Count > 0)
                lines.Add($"- Nice-to-Have: {string.Join("; ", data.JdContent.NiceToHave)}");

            return string.Join("\n", lines);
        }

        /// <summary>
        /// TryParse cho response mảng câu hỏi lồng nhau — validate cả cấu trúc bên trong.
        /// </summary>
        private static bool TryParseQuestions(string json, out List<QuestionBankGenerationResult>? results, out string? error)
        {
            results = null;
            error = null;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<QuestionBankWrapper>(json, options);

                if (wrapper?.Questions == null || wrapper.Questions.Count == 0)
                {
                    error = "Questions array is null or empty";
                    return false;
                }

                if (wrapper.Questions.Count < 3)
                {
                    error = $"Expected at least 3 questions, got {wrapper.Questions.Count}";
                    return false;
                }

                // Validate từng câu hỏi
                for (int i = 0; i < wrapper.Questions.Count; i++)
                {
                    var q = wrapper.Questions[i];
                    if (string.IsNullOrWhiteSpace(q.Question))
                    {
                        error = $"Question [{i}] has empty question text";
                        return false;
                    }

                    if (q.ScoringRubric == null)
                    {
                        error = $"Question [{i}] has null ScoringRubric";
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(q.ScoringRubric.Excellent) ||
                        string.IsNullOrWhiteSpace(q.ScoringRubric.Good) ||
                        string.IsNullOrWhiteSpace(q.ScoringRubric.Acceptable) ||
                        string.IsNullOrWhiteSpace(q.ScoringRubric.Poor))
                    {
                        error = $"Question [{i}] has incomplete ScoringRubric (all 4 levels required)";
                        return false;
                    }
                }

                // Map sang result model
                results = new List<QuestionBankGenerationResult>();
                foreach (var q in wrapper.Questions)
                {
                    Enum.TryParse<QuestionCategory>(q.Category, true, out var category);
                    Enum.TryParse<JobLevel>(q.Difficulty, true, out var difficulty);

                    results.Add(new QuestionBankGenerationResult
                    {
                        Question = q.Question,
                        Category = category,
                        Difficulty = difficulty,
                        ScoringRubric = new ScoringRubric
                        {
                            Excellent = q.ScoringRubric!.Excellent,
                            Good = q.ScoringRubric.Good,
                            Acceptable = q.ScoringRubric.Acceptable,
                            Poor = q.ScoringRubric.Poor
                        }
                    });
                }

                return true;
            }
            catch (JsonException ex)
            {
                error = $"JSON parse error: {ex.Message}";
                return false;
            }
        }

        // Wrapper classes chỉ dùng cho deserialization — tách biệt khỏi domain models
        private class QuestionBankWrapper
        {
            public List<QuestionRaw> Questions { get; set; } = new();
        }

        private class QuestionRaw
        {
            public string Question { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Difficulty { get; set; } = string.Empty;
            public ScoringRubricRaw? ScoringRubric { get; set; }
        }

        private class ScoringRubricRaw
        {
            public string Excellent { get; set; } = string.Empty;
            public string Good { get; set; } = string.Empty;
            public string Acceptable { get; set; } = string.Empty;
            public string Poor { get; set; } = string.Empty;
        }
    }
}
