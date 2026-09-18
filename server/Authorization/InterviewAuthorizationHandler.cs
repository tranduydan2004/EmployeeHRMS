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
    /// Candidate: Succeed Read nếu interview.Application.Candidate.UserId == currentUserId.
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

            // Candidate -> Succeed Read nếu interview thuộc Application của Candidate sở hữu
            if (role == nameof(UserRole.Candidate) && requirement.Name == nameof(ResourceOperations.Read))
            {
                var candidateUserId = await _context.Applications
                    .Where(a => a.Id == resource.ApplicationId)
                    .Select(a => a.Candidate.UserId)
                    .FirstOrDefaultAsync();

                if (candidateUserId.HasValue && candidateUserId.Value == currentUserId)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
