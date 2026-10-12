using System;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Configuration
{
    public class AudioInterviewOptions
    {
        public const string SectionName = "AudioInterview";

        public int DefaultThinkSeconds { get; set; } = 20;
        public int DefaultAnswerSeconds { get; set; } = 90;
        public int DurationBufferSeconds { get; set; } = 5;
        public int DefaultInviteExpiryDays { get; set; } = 3;
        public string BaseCandidatePortalUrl { get; set; } = "http://localhost:5173/interview";

        // === Khoảng hợp lệ duy nhất của toàn hệ thống (Single Source of Truth) ===
        public int MinThinkSeconds { get; set; } = 5;
        public int MaxThinkSeconds { get; set; } = 40;
        public int MinAnswerSeconds { get; set; } = 10;
        public int MaxAnswerSeconds { get; set; } = 120;

        // === Bảng quy tắc gợi ý theo Category & Difficulty (có thể ghi đè trong appsettings) ===
        // Technical, Intern/Fresher/Junior/Middle: 20 / 60
        public int TechnicalJuniorThinkSeconds { get; set; } = 20;
        public int TechnicalJuniorAnswerSeconds { get; set; } = 60;

        // Technical, Senior/Lead: 30 / 90
        public int TechnicalSeniorThinkSeconds { get; set; } = 30;
        public int TechnicalSeniorAnswerSeconds { get; set; } = 90;

        // Situational (mọi level): 20 / 90
        public int SituationalThinkSeconds { get; set; } = 20;
        public int SituationalAnswerSeconds { get; set; } = 90;

        // Behavioral: 15 / 60
        public int BehavioralThinkSeconds { get; set; } = 15;
        public int BehavioralAnswerSeconds { get; set; } = 60;

        // CulturalFit: 15 / 45
        public int CulturalFitThinkSeconds { get; set; } = 15;
        public int CulturalFitAnswerSeconds { get; set; } = 45;

        /// <summary>
        /// Hàm thuần gợi ý thời gian suy nghĩ & trả lời theo Category và Difficulty,
        /// tự động clamp vào khoảng [Min, Max] hợp lệ của hệ thống.
        /// </summary>
        public (int ThinkSeconds, int AnswerSeconds) SuggestTimeLimit(QuestionCategory category, JobLevel difficulty)
        {
            int think;
            int answer;

            switch (category)
            {
                case QuestionCategory.Technical:
                    if (difficulty == JobLevel.Senior || difficulty == JobLevel.Lead)
                    {
                        think = TechnicalSeniorThinkSeconds;
                        answer = TechnicalSeniorAnswerSeconds;
                    }
                    else
                    {
                        think = TechnicalJuniorThinkSeconds;
                        answer = TechnicalJuniorAnswerSeconds;
                    }
                    break;

                case QuestionCategory.Situational:
                    think = SituationalThinkSeconds;
                    answer = SituationalAnswerSeconds;
                    break;

                case QuestionCategory.Behavioral:
                    think = BehavioralThinkSeconds;
                    answer = BehavioralAnswerSeconds;
                    break;

                case QuestionCategory.CulturalFit:
                    think = CulturalFitThinkSeconds;
                    answer = CulturalFitAnswerSeconds;
                    break;

                default:
                    think = DefaultThinkSeconds;
                    answer = DefaultAnswerSeconds;
                    break;
            }

            // Clamp vào khoảng hợp lệ
            think = Math.Clamp(think, MinThinkSeconds, MaxThinkSeconds);
            answer = Math.Clamp(answer, MinAnswerSeconds, MaxAnswerSeconds);

            return (think, answer);
        }

        public bool IsThinkSecondsValid(int? seconds) =>
            !seconds.HasValue || (seconds.Value >= MinThinkSeconds && seconds.Value <= MaxThinkSeconds);

        public bool IsAnswerSecondsValid(int? seconds) =>
            !seconds.HasValue || (seconds.Value >= MinAnswerSeconds && seconds.Value <= MaxAnswerSeconds);
    }
}
