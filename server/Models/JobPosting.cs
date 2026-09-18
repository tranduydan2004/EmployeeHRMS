namespace EmployeeHRMS.Api.Models
{

    public enum JobPostingStatus
    {
        Draft = 1,
        Published = 2,
        Closed = 3
    }

    public class JobPosting // Tin tuyển dụng
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // VD: Senior .NET Developer

        // Các trường này AI sẽ tự động sinh ra (Format dạng HTML/Markdown)
        public string? Description { get; set; }
        public string? Requirements { get; set; }

        public JobPostingStatus Status { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Foreign key
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
