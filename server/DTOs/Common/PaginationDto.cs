using System.ComponentModel.DataAnnotations;

namespace EmployeeHRMS.Api.DTOs.Common
{
    /// <summary>
    /// Generic pagination + sorting parameters.
    /// Controller nhận [FromQuery] PaginationParams.
    /// </summary>
    public class PaginationParams
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1.")]
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        [Range(1, 50, ErrorMessage = "Page size must be between 1 and 50.")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 1 : (value > 50 ? 50 : value);
        }

        /// <summary>
        /// Tên cột sắp xếp (whitelist-validated trong Service).
        /// Null/empty → fallback sắp xếp mặc định.
        /// </summary>
        public string? SortBy { get; set; }

        public bool IsDescending { get; set; } = false;

        /// <summary>
        /// Từ khóa tìm kiếm (optional, case-insensitive ILike trên PostgreSQL).
        /// </summary>
        public string? Search { get; set; }
    }

    /// <summary>
    /// Generic paged result wrapper.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
