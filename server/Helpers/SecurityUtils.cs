using System.Security.Cryptography;
using System.Text;

namespace EmployeeHRMS.Api.Helpers
{
    public static class SecurityUtils
    {
        public static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
