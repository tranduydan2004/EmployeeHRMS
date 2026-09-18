using System.Security.Claims;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Api.Authorization
{
    /// <summary>
    /// EmployeeAuthorizationHandler — Resource-Based Authorization cho Employee entity.
    /// Admin, HR: Succeed mọi operation (Read, Create, Update, Delete).
    /// Employee: Chỉ Succeed Read nếu employee.EmployeeId == user.EmployeeId (xem hồ sơ chính mình).
    /// </summary>
    public class EmployeeAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, Employee>
    {
        private readonly AppDbContext _context;

        public EmployeeAuthorizationHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Employee resource)
        {
            var role = context.User.FindFirstValue(ClaimTypes.Role);

            // Admin, HR -> Succeed mọi operation
            if (role is nameof(UserRole.Admin) or nameof(UserRole.HR))
            {
                context.Succeed(requirement);
                return;
            }

            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var currentUserId)) return;

            // Employee -> Chỉ được Read hồ sơ của chính mình
            if (role == nameof(UserRole.Employee) && requirement.Name == nameof(ResourceOperations.Read))
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);

                if (user != null && user.EmployeeId.HasValue && user.EmployeeId.Value == resource.EmployeeId)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
