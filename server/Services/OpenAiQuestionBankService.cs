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
            You are a senior HR and technical interview specialist for an enterprise HRMS system.
            Your task is to generate 3-5 situational and technical interview questions with objective, checklist-based scoring rubrics for automated AI candidate evaluation.

            RULES:
            1. QUESTION SPECIFICATIONS:
               - Generate 3-5 questions that directly assess the skills and key responsibilities in the JD.
               - Mix categories: include at least 1 Technical question and at least 1 Behavioral/Situational question.
               - Difficulty calibration: Match question complexity to the candidate's level:
                 * Intern/Fresher/Junior: Core fundamentals, standard language/framework APIs, basic debugging, edge cases, and clean code.
                 * Middle: Design patterns, performance nuances, edge cases, structured error recovery.
                 * Senior/Lead: Architectural trade-offs, scalability, concurrency, fault tolerance, and technical leadership.

            2. CRITICAL RULES FOR SCORING RUBRICS (NO VAGUE ADJECTIVES):
               - NEVER use subjective or qualitative adjectives like "good understanding", "solid knowledge", "lacks depth", "expert level", or "satisfactory".
               - Every rubric level MUST use concrete, observable checkpoints and mutually exclusive conditions tailored to that specific question:

               FOR TECHNICAL QUESTIONS:
               * Excellent (9-10 pts): Explicitly states all mandatory technical concepts/mechanics AND discusses at least one concrete architectural trade-off, performance nuance, edge case, or production-grade mitigation (calibrated to the role's level).
               * Good (7-8 pts): Accurately explains all mandatory core concepts and mechanics without factual errors, but omits advanced edge cases or architectural trade-offs.
               * Acceptable (5-6 pts): Identifies at least 1-2 core concepts correctly, but shows partial gaps, minor confusion, or omits key mechanisms; has NO fundamental factual misconceptions.
               * Poor (1-4 pts): Gives factually incorrect statements, fundamental architectural misconceptions, confuses basic terms, or cannot answer.

               FOR BEHAVIORAL / SITUATIONAL QUESTIONS:
               * Excellent (9-10 pts): Complete STAR structure (Situation, Task, Action, Result). Demonstrates clear individual ownership ("I" vs "we"), explains specific decision rationale, provides measurable outcomes/impact, and shares retrospective learnings or preventive actions.
               * Good (7-8 pts): Clear STAR structure with specific personal actions and a positive resolution, but lacks quantifiable metrics or retrospective lessons learned.
               * Acceptable (5-6 pts): Describes a relevant situation and general outcome, but actions are vague or passive, relies heavily on generic team actions ("we") without clarifying individual contribution.
               * Poor (1-4 pts): Fails to provide a concrete past experience, blames others, demonstrates unprofessional or uncollaborative conduct, or provides no actionable resolution.

            3. LANGUAGE:
               - Output all questions and scoring rubrics strictly in English.
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
                        "question": "Walk us through how you would diagnose and resolve an intermittent 500 Internal Server Error in an ASP.NET Core Web API under high load.",
                        "category": "Technical",
                        "difficulty": "Junior",
                        "scoringRubric": {
                            "excellent": "Explicitly details a systematic workflow: checking structured logs/correlation IDs, inspecting APM/exception stack traces, and determining whether the root cause is DB connection pool starvation, timeout, or unhandled exceptions. Additionally mentions preventive measures (e.g., adding health checks, automated regression tests, or alerting).",
                            "good": "Accurately details a step-by-step diagnostic workflow: inspecting server error logs, reproducing with matching payload in staging, fixing the code path, and verifying. Factual without misconceptions, but omits connection pool exhaustion, timeout nuances, or long-term monitoring.",
                            "acceptable": "Identifies basic troubleshooting techniques such as reading console output or stepping through code with a local debugger, but lacks a disciplined server-side diagnostic mindset (e.g., no mention of correlation IDs, log aggregation, or production safety).",
                            "poor": "Cannot articulate a debugging methodology, suggests blindly pushing trial-and-error fixes directly to production, or confuses client-side (4xx) errors with server-side (5xx) errors."
                        }
                    },
                    {
                        "question": "Describe a situation where you had a technical disagreement with a team member regarding an API contract or schema design. How did you resolve it?",
                        "category": "Behavioral",
                        "difficulty": "Junior",
                        "scoringRubric": {
                            "excellent": "Uses STAR method: details a specific conflict over schema/API design, explains their personal role in scheduling an objective discussion, evaluates options against backward compatibility and consumer needs, reaches consensus, and shares a clear takeaway on team communication.",
                            "good": "Uses STAR method: describes a real technical disagreement, explains how they communicated calmly to compare pros and cons, and reached a working agreement, but does not reflect on broader team processes or backward-compatibility implications.",
                            "acceptable": "Describes a disagreement and the eventual resolution, but actions are passive or attributes the final decision entirely to a lead/manager without explaining their own reasoning.",
                            "poor": "Fails to provide a concrete scenario, admits to unilaterally overriding the colleague without discussion, displays uncollaborative behavior, or holds unaddressed resentment."
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
