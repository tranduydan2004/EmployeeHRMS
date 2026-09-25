namespace EmployeeHRMS.Api.Models.ValueObjects
{
    /// <summary>
    /// Value Object đại diện cho barem chấm điểm câu hỏi phỏng vấn gồm 4 mức.
    /// Map sang cột jsonb qua EF Core 8 ToJson() Owned Entity.
    /// </summary>
    public class ScoringRubric
    {
        public string Excellent { get; set; } = string.Empty;
        public string Good { get; set; } = string.Empty;
        public string Acceptable { get; set; } = string.Empty;
        public string Poor { get; set; } = string.Empty;
    }
}
