using System.Security.Claims;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Api.Authorization
{
    /// <summary>
    /// InterviewAuthorizationHandler — dùng cho GetById/Create/Delete/Evaluate.
    /// Admin, HR: Succeed mọi operation.
    /// Interviewer: Succeed Read/Evaluate nếu interview.InterviewerId == currentUserId.
    /// Candidate: Tuyệt đối KHÔNG có quyền truy cập vào Interview dashboard resource.
    /// </summary>
    public class InterviewAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, Interview>
    {
        private readonly AppDbContext _context;

        public InterviewAuthorizationHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Interview resource)
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return;

            var currentUserId = int.Parse(userIdClaim);
            var role = context.User.FindFirstValue(ClaimTypes.Role);

            // Admin, HR -> Succeed mọi operation
            if (role is nameof(UserRole.Admin) or nameof(UserRole.HR))
            {
                context.Succeed(requirement);
                return;
            }

            // Interviewer -> Succeed Read/Evaluate nếu là interviewer được gán
            if (role == nameof(UserRole.Interviewer))
            {
                if (resource.InterviewerId == currentUserId &&
                    requirement.Name is nameof(ResourceOperations.Read) or nameof(ResourceOperations.Evaluate))
                {
                    context.Succeed(requirement);
                }
                return;
            }

            // Candidate/Employee/Anonymous: Không cấp quyền Read hay Evaluate trên Interview entity
            await Task.CompletedTask;
        }
    }
}
