namespace EmployeeHRMS.Api.Models
{
    public enum EmployeeStatus
    {
        Probation = 0,
        Active = 1,
        OnLeave = 2,
        Terminated = 3
    }

    /// <summary>
    /// Employee entity — DDD/Anemic hybrid: có method ChangeStatus để bảo vệ encapsulation.
    /// EF Core tự sinh EmployeeId (DB auto-increment).
    /// </summary>
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty; // VD: Backend Developer
        public DateTime JoinDate { get; set; }
        public decimal Salary { get; set; }
        public DateTime CreatedAt { get; set; }

        public EmployeeStatus Status { get; set; }

        // Foreign key
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        /// <summary>
        /// Cập nhật trạng thái nhân viên
        /// </summary>
        public void ChangeStatus(EmployeeStatus newStatus)
        {
            Status = newStatus;
        }
    }
}
