using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace EmployeeHRMS.Api.Authorization
{
    /// <summary>
    /// Định nghĩa các operation dùng cho Resource-Based Authorization.
    /// Mỗi OperationAuthorizationRequirement đại diện cho 1 loại hành vi.
    /// </summary>
    public static class ResourceOperations
    {
        public static readonly OperationAuthorizationRequirement Read =
            new() { Name = nameof(Read) };

        public static readonly OperationAuthorizationRequirement Create =
            new() { Name = nameof(Create) };

        public static readonly OperationAuthorizationRequirement Update =
            new() { Name = nameof(Update) };

        public static readonly OperationAuthorizationRequirement Delete =
            new() { Name = nameof(Delete) };

        /// <summary>
        /// Evaluate: dành riêng cho Interview — thêm câu hỏi, chấm điểm.
        /// </summary>
        public static readonly OperationAuthorizationRequirement Evaluate =
            new() { Name = nameof(Evaluate) };
    }
}
