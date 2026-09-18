using System.Collections.Generic;
using EmployeeHRMS.Api.Authorization;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeHRMS.Api.Controllers
{
    /// <summary>
    /// Applications Controller — CRUD + nghiệp vụ chuyển trạng thái + filter
    /// Resource-Based Authorization: GetById/Create/Delete dùng IAuthorizationService.
    /// UpdateStatus: [Authorize(Roles = "Admin,HR")] đơn giản.
    /// GetAll lọc tự động theo Role trong Service layer.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Route: api/applications
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationService _applicationService;
        private readonly ICandidateService _candidateService;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationService _notificationService;

        public ApplicationsController(
            IApplicationService applicationService,
            ICandidateService candidateService,
            IAuthorizationService authorizationService,
            INotificationService notificationService)
        {
            _applicationService = applicationService;
            _candidateService = candidateService;
            _authorizationService = authorizationService;
            _notificationService = notificationService;
        }

        // GET: api/applications — Lọc tự động theo Role trong Service
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
        {
            var result = await _applicationService.GetAllAsync(paginationParams);
            return Ok(result);
        }

        // GET: api/applications/{id} — Resource-Based Authorization
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var application = await _applicationService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Application", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, application, ResourceOperations.Read);
            if (!authResult.Succeeded) return Forbid();

            var dto = await _applicationService.GetByIdAsync(id);
            return Ok(dto);
        }

        // GET: api/applications/by-status/{status}
        [HttpGet("by-status/{status}")]
        public async Task<IActionResult> GetByStatus(ApplicationStatus status)
        {
            var applications = await _applicationService.GetByStatusAsync(status);
            return Ok(applications);
        }

        // GET: api/applications/by-candidate/{candidateId}
        [HttpGet("by-candidate/{candidateId}")]
        public async Task<IActionResult> GetByCandidate(int candidateId)
        {
            var applications = await _applicationService.GetByCandidateAsync(candidateId);
            return Ok(applications);
        }

        // GET: api/applications/by-job/{jobPostingId}
        [HttpGet("by-job/{jobPostingId}")]
        public async Task<IActionResult> GetByJobPosting(int jobPostingId)
        {
            var applications = await _applicationService.GetByJobPostingAsync(jobPostingId);
            return Ok(applications);
        }

        // POST: api/applications — Resource-Based Authorization (Candidate chỉ tạo đơn cho chính mình)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ApplicationCreateDto dto)
        {
            // Fetch Candidate entity để kiểm tra quyền tạo đơn
            var candidate = await _candidateService.GetEntityByIdAsync(dto.CandidateId)
                ?? throw new BusinessRuleException($"Candidate with ID {dto.CandidateId} does not exist.");

            var authResult = await _authorizationService.AuthorizeAsync(User, candidate, ResourceOperations.Create);
            if (!authResult.Succeeded) return Forbid();

            var application = await _applicationService.CreateAsync(dto);

            // Gửi thông báo tới Admin và HR
            await _notificationService.SendToRolesAsync(
                new[] { UserRole.Admin, UserRole.HR },
                "Hồ sơ ứng tuyển mới",
                "APPLICATION_SUBMITTED",
                new Dictionary<string, string>
                {
                    { "candidateName", candidate.FullName },
                    { "jobTitle", application.JobTitle }
                },
                NotificationType.Info,
                relatedEntity: "Application",
                relatedEntityId: application.Id,
                fallbackMessage: $"Ứng viên {candidate.FullName} vừa nộp hồ sơ cho vị trí {application.JobTitle}.");

            return CreatedAtAction(nameof(GetById), new { id = application.Id }, application);
        }

        // PATCH: api/applications/{id}/status — Chỉ Admin/HR mới đổi trạng thái
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ApplicationUpdateStatusDto dto)
        {
            await _applicationService.UpdateStatusAsync(id, dto.Status);

            var application = await _applicationService.GetEntityByIdAsync(id);
            if (application?.Candidate?.UserId != null)
            {
                var notifType = dto.Status == ApplicationStatus.Offered
                    ? NotificationType.Success
                    : dto.Status == ApplicationStatus.Rejected
                        ? NotificationType.Warning
                        : NotificationType.Info;

                await _notificationService.SendToUserAsync(
                    application.Candidate.UserId.Value,
                    "Cập nhật trạng thái hồ sơ",
                    "APPLICATION_STATUS_CHANGED",
                    new Dictionary<string, string>
                    {
                        { "jobTitle", application.JobPosting?.Title ?? "công việc" },
                        { "status", dto.Status.ToString() }
                    },
                    notifType,
                    relatedEntity: "Application",
                    relatedEntityId: id,
                    fallbackMessage: $"Hồ sơ ứng tuyển của bạn cho vị trí {application.JobPosting?.Title ?? "công việc"} đã được cập nhật sang trạng thái: {dto.Status}.");
            }

            return NoContent();
        }

        // DELETE: api/applications/{id} — Resource-Based Authorization
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var application = await _applicationService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Application", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, application, ResourceOperations.Delete);
            if (!authResult.Succeeded) return Forbid();

            await _applicationService.DeleteAsync(id);
            return NoContent();
        }
    }
}
