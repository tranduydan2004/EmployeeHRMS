using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface cho JWT Token generation.
    /// </summary>
    public interface IJwtService
    {
        (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user);
        string GenerateRefreshToken();
    }
}
