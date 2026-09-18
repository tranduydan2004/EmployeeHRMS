namespace EmployeeHRMS.Api.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // VD: IT, HR, Marketing

        // Navigation properties
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }
}
