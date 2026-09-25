namespace EmployeeHRMS.Api.Models.ValueObjects
{
    public enum Currency
    {
        VND = 1,
        USD = 2,
        EUR = 3
    }

    /// <summary>
    /// Value Object đại diện cho khoảng lương tuyển dụng.
    /// Map dạng Owned Entity trong JobPosting.
    /// </summary>
    public class SalaryRange
    {
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public Currency Currency { get; set; } = Currency.VND;
    }
}
