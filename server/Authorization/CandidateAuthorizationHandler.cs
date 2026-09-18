using System.Security.Claims;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Api.Authorization
{
    /// <summary>
    /// CandidateAuthorizationHandler — dùng cho GetById/Update/Delete.
    /// Admin, HR: Succeed mọi operation.
    /// Candidate: Succeed nếu candidate.UserId == currentUserId.
    /// Interviewer: Succeed Read nếu Candidate có Application → Interview do Interviewer phụ trách.
    /// </summary>
    public class CandidateAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, Candidate>
    {
        private readonly AppDbContext _context;

        public CandidateAuthorizationHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Candidate resource)
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

            // Candidate -> Succeed nếu sở hữu resource
            if (role == nameof(UserRole.Candidate))
            {
                if (resource.UserId.HasValue && resource.UserId.Value == currentUserId)
                {
                    context.Succeed(requirement);
                }
                return;
            }

            // Interviewer -> Succeed CHỈ với Read, nếu Candidate có Application → Interview
            // mà InterviewerId == currentUserId
            if (role == nameof(UserRole.Interviewer) && requirement.Name == nameof(ResourceOperations.Read))
            {
                var hasRelatedInterview = await _context.Interviews
                    .AnyAsync(i =>
                        i.InterviewerId == currentUserId &&
                        i.Application.CandidateId == resource.Id);

                if (hasRelatedInterview)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
