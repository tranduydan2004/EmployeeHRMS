using System.Net;

namespace EmployeeHRMS.Api.Exceptions
{
    /// <summary>
    /// Ngoại lệ khi không tìm thấy entity → HTTP 404 Not Found.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string entityName, object id)
            : base($"{entityName} with ID {id} not found.") { }
    }

    /// <summary>
    /// Ngoại lệ khi vi phạm nghiệp vụ hoặc ràng buộc FK → HTTP 400 Bad Request (hoặc mã HttpStatusCode tùy biến).
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public BusinessRuleException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Ngoại lệ khi dữ liệu bị trùng lặp → HTTP 409 Conflict.
    /// </summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
