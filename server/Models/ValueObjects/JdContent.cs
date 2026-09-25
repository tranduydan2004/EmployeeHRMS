using System.Collections.Generic;

namespace EmployeeHRMS.Api.Models.ValueObjects
{
    /// <summary>
    /// Value Object đại diện cho nội dung JD cấu trúc 5 phần do LLM sinh ra.
    /// Map sang cột jsonb qua EF Core 8 ToJson() Owned Entity.
    /// </summary>
    public class JdContent
    {
        public string Intro { get; set; } = string.Empty;
        public List<string> Responsibilities { get; set; } = new();
        public List<string> MustHave { get; set; } = new();
        public List<string> NiceToHave { get; set; } = new();
        public List<string> Benefits { get; set; } = new();
    }
}
