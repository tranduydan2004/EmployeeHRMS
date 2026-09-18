using System.Security.Claims;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHRMS.Api.Authorization
{
    /// <summary>
    /// ApplicationAuthorizationHandler — dùng cho GetById/Create/Update/Delete.
    /// Admin, HR: Succeed mọi operation.
    /// Candidate: Succeed Read/Create nếu application.Candidate.UserId == currentUserId.
    /// Interviewer: Succeed Read nếu Application thuộc Interview do Interviewer phụ trách.
    /// </summary>
    public class ApplicationAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, Application>
    {
        private readonly AppDbContext _context;

        public ApplicationAuthorizationHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Application resource)
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

            // Candidate -> Succeed Read/Create/Delete nếu application thuộc Candidate sở hữu
            if (role == nameof(UserRole.Candidate))
            {
                // Cần load Candidate.UserId nếu chưa include
                var candidateUserId = resource.Candidate?.UserId
                    ?? await _context.Candidates
                        .Where(c => c.Id == resource.CandidateId)
                        .Select(c => c.UserId)
                        .FirstOrDefaultAsync();

                if (candidateUserId.HasValue && candidateUserId.Value == currentUserId)
                {
                    if (requirement.Name is nameof(ResourceOperations.Read)
                        or nameof(ResourceOperations.Create)
                        or nameof(ResourceOperations.Delete))
                    {
                        context.Succeed(requirement);
                    }
                }
                return;
            }

            // Interviewer -> Succeed Read nếu Application thuộc Interview do Interviewer phụ trách
            if (role == nameof(UserRole.Interviewer) && requirement.Name == nameof(ResourceOperations.Read))
            {
                var hasRelatedInterview = await _context.Interviews
                    .AnyAsync(i =>
                        i.InterviewerId == currentUserId &&
                        i.ApplicationId == resource.Id);

                if (hasRelatedInterview)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
