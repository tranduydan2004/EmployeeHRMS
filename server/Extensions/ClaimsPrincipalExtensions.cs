using System.Security.Claims;

namespace EmployeeHRMS.Api.Extensions
{
    /// <summary>
    /// Extension methods cho ClaimsPrincipal — parse UserId/Role từ JWT claims.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Lấy UserId (int) từ ClaimTypes.NameIdentifier.
        /// Trả về null nếu không có claim hoặc parse thất bại.
        /// </summary>
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }

        /// <summary>
        /// Lấy Role string từ ClaimTypes.Role.
        /// </summary>
        public static string? GetRole(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Role);
        }
    }
}
