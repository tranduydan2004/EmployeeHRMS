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
    /// Interviews Controller — CRUD + quản lý câu hỏi phỏng vấn
    /// Resource-Based Authorization: GetById/Delete dùng IAuthorizationService.
    /// AddQuestion dùng ResourceOperations.Evaluate.
    /// Create: [Authorize(Roles = "Admin,HR")] — chỉ Admin/HR tạo lịch phỏng vấn.
    /// GetAll lọc tự động theo Role trong Service layer.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Route: api/interviews
    [Authorize]
    public class InterviewsController : ControllerBase
    {
        private readonly IInterviewService _interviewService;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationService _notificationService;
        private readonly IAudioIngestionService _audioIngestionService;

        public InterviewsController(
            IInterviewService interviewService,
            IAuthorizationService authorizationService,
            INotificationService notificationService,
            IAudioIngestionService audioIngestionService)
        {
            _interviewService = interviewService;
            _authorizationService = authorizationService;
            _notificationService = notificationService;
            _audioIngestionService = audioIngestionService;
        }

        // GET: api/interviews — Lọc tự động theo Role trong Service
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
        {
            var interviews = await _interviewService.GetAllAsync(paginationParams);
            return Ok(interviews);
        }

        // GET: api/interviews/{id} — Resource-Based Authorization
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var interview = await _interviewService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Interview", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, interview, ResourceOperations.Read);
            if (!authResult.Succeeded) return Forbid();

            var dto = await _interviewService.GetByIdAsync(id);
            return Ok(dto);
        }

        // GET: api/interviews/by-application/{applicationId}
        [HttpGet("by-application/{applicationId}")]
        public async Task<IActionResult> GetByApplication(int applicationId)
        {
            var interviews = await _interviewService.GetByApplicationAsync(applicationId);
            return Ok(interviews);
        }

        // POST: api/interviews — Chỉ Admin/HR tạo lịch phỏng vấn
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create([FromBody] InterviewCreateDto dto)
        {
            var interview = await _interviewService.CreateAsync(dto);

            // Gửi thông báo tới Candidate (nếu có UserId) và Interviewer (nếu có InterviewerId)
            var entity = await _interviewService.GetEntityByIdAsync(interview.Id);
            var candidateUserId = entity?.Application?.Candidate?.UserId;
            var candidateName = entity?.Application?.Candidate?.FullName ?? "Ứng viên";

            if (candidateUserId.HasValue)
            {
                await _notificationService.SendToUserAsync(
                    candidateUserId.Value,
                    "Lịch phỏng vấn mới",
                    "INTERVIEW_SCHEDULED_CANDIDATE",
                    new Dictionary<string, string>
                    {
                        { "scheduledDate", interview.ScheduledDate.ToString("dd/MM/yyyy HH:mm") }
                    },
                    NotificationType.ActionRequired,
                    relatedEntity: "Interview",
                    relatedEntityId: interview.Id,
                    fallbackMessage: $"Bạn có lịch phỏng vấn mới vào lúc {interview.ScheduledDate:dd/MM/yyyy HH:mm}.");
            }

            if (dto.InterviewerId.HasValue)
            {
                await _notificationService.SendToUserAsync(
                    dto.InterviewerId.Value,
                    "Phân công phỏng vấn",
                    "INTERVIEW_ASSIGNED_INTERVIEWER",
                    new Dictionary<string, string>
                    {
                        { "candidateName", candidateName },
                        { "scheduledDate", interview.ScheduledDate.ToString("dd/MM/yyyy HH:mm") }
                    },
                    NotificationType.ActionRequired,
                    relatedEntity: "Interview",
                    relatedEntityId: interview.Id,
                    fallbackMessage: $"Bạn được phân công phỏng vấn ứng viên {candidateName} vào lúc {interview.ScheduledDate:dd/MM/yyyy HH:mm}.");
            }

            return CreatedAtAction(nameof(GetById), new { id = interview.Id }, interview);
        }

        // PATCH: api/interviews/{id}/complete — Hoàn tất phỏng vấn (Admin, HR, Interviewer)
        [HttpPatch("{id}/complete")]
        [Authorize(Roles = "Admin,HR,Interviewer")]
        public async Task<IActionResult> Complete(int id, [FromBody] InterviewCompleteDto? dto = null)
        {
            var interview = await _interviewService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Interview", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, interview, ResourceOperations.Evaluate);
            if (!authResult.Succeeded) return Forbid();

            var result = await _interviewService.CompleteInterviewAsync(id, dto?.Summary);
            if (result == null) throw new NotFoundException("Interview", id);

            var candidateName = interview.Application?.Candidate?.FullName ?? "Ứng viên";

            await _notificationService.SendToRolesAsync(
                new[] { UserRole.Admin, UserRole.HR },
                "Hoàn tất phỏng vấn",
                "INTERVIEW_COMPLETED",
                new Dictionary<string, string>
                {
                    { "interviewId", id.ToString() },
                    { "candidateName", candidateName }
                },
                NotificationType.Success,
                relatedEntity: "Interview",
                relatedEntityId: id,
                fallbackMessage: $"Buổi phỏng vấn #{id} của ứng viên {candidateName} đã được hoàn tất.");

            return Ok(result);
        }

        // POST: api/interviews/{interviewId}/questions — Resource-Based Authorization (Evaluate)
        [HttpPost("{interviewId}/questions")]
        public async Task<IActionResult> AddQuestion(int interviewId, [FromBody] InterviewQuestionCreateDto dto)
        {
            var interview = await _interviewService.GetEntityByIdAsync(interviewId)
                ?? throw new NotFoundException("Interview", interviewId);

            var authResult = await _authorizationService.AuthorizeAsync(User, interview, ResourceOperations.Evaluate);
            if (!authResult.Succeeded) return Forbid();

            await _interviewService.AddQuestionAsync(interviewId, dto);
            return Ok(new { Message = "Question added successfully." });
        }

        // PATCH: api/interviews/{interviewId}/questions/{questionId}/answer — Resource-Based Authorization (Evaluate)
        [HttpPatch("{interviewId}/questions/{questionId}/answer")]
        public async Task<IActionResult> UpdateQuestionAnswer(int interviewId, int questionId, [FromBody] InterviewQuestionAnswerDto dto)
        {
            var interview = await _interviewService.GetEntityWithQuestionsAsync(interviewId)
                ?? throw new NotFoundException("Interview", interviewId);

            var authResult = await _authorizationService.AuthorizeAsync(User, interview, ResourceOperations.Evaluate);
            if (!authResult.Succeeded) return Forbid();

            var question = interview.Questions.FirstOrDefault(q => q.Id == questionId)
                ?? throw new NotFoundException("InterviewQuestion", questionId);

            await _interviewService.UpdateQuestionAnswerAsync(question, dto.CandidateAnswer);
            return Ok(new { message = "Cập nhật câu trả lời thành công." });
        }

        // DELETE: api/interviews/{id} — Resource-Based Authorization
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var interview = await _interviewService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Interview", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, interview, ResourceOperations.Delete);
            if (!authResult.Succeeded) return Forbid();

            var candidateUserId = interview.Application?.Candidate?.UserId;
            var candidateName = interview.Application?.Candidate?.FullName ?? "Ứng viên";
            var jobTitle = interview.Application?.JobPosting?.Title ?? "buổi phỏng vấn";
            var scheduledDate = interview.ScheduledDate;
            var interviewerId = interview.InterviewerId;

            await _interviewService.DeleteAsync(id);

            // Gửi thông báo tới Ứng viên (nếu có tài khoản User liên kết)
            if (candidateUserId.HasValue)
            {
                await _notificationService.SendToUserAsync(
                    candidateUserId.Value,
                    "Hủy lịch phỏng vấn",
                    "INTERVIEW_CANCELLED_CANDIDATE",
                    new Dictionary<string, string>
                    {
                        { "jobTitle", jobTitle },
                        { "scheduledDate", scheduledDate.ToString("dd/MM/yyyy HH:mm") }
                    },
                    NotificationType.Warning,
                    relatedEntity: "Interview",
                    relatedEntityId: id,
                    fallbackMessage: $"Lịch phỏng vấn cho vị trí {jobTitle} vào lúc {scheduledDate:dd/MM/yyyy HH:mm} đã bị hủy.");
            }

            // Gửi thông báo tới Người phỏng vấn (nếu đã phân công)
            if (interviewerId.HasValue)
            {
                await _notificationService.SendToUserAsync(
                    interviewerId.Value,
                    "Hủy lịch phỏng vấn",
                    "INTERVIEW_CANCELLED_INTERVIEWER",
                    new Dictionary<string, string>
                    {
                        { "candidateName", candidateName },
                        { "scheduledDate", scheduledDate.ToString("dd/MM/yyyy HH:mm") }
                    },
                    NotificationType.Warning,
                    relatedEntity: "Interview",
                    relatedEntityId: id,
                    fallbackMessage: $"Lịch phỏng vấn ứng viên {candidateName} vào lúc {scheduledDate:dd/MM/yyyy HH:mm} đã bị hủy.");
            }

            return NoContent();
        }

        // ============================================================
        // Audio Interview Ingestion & Status Polling (Phase 3)
        // ============================================================

        /// <summary>
        /// POST: api/interviews/{inviteToken}/answers/{questionId}
        /// Ứng viên nộp file ghi âm trả lời (multipart/form-data) kèm telemetry chống gian lận.
        /// Public access với InviteToken bảo mật (chống IDOR).
        /// Tiếp nhận audio, validate 4 lớp, lưu storage, lưu DB (Pending), và enqueue vào Channel queue.
        /// </summary>
        [HttpPost("{inviteToken:guid}/answers/{questionId:int}")]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitAnswerAudio(
            Guid inviteToken,
            int questionId,
            [FromForm] AudioAnswerUploadDto dto,
            CancellationToken cancellationToken)
        {
            var result = await _audioIngestionService.ProcessAudioUploadAsync(
                inviteToken,
                questionId,
                dto,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetAnswerStatus),
                new { inviteToken, questionId },
                result);
        }

        /// <summary>
        /// GET: api/interviews/{inviteToken}/answers/{questionId}
        /// Endpoint polling trạng thái xử lý audio của câu trả lời (Pending -> Transcribing -> Scoring -> Completed / Failed).
        /// </summary>
        [HttpGet("{inviteToken:guid}/answers/{questionId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAnswerStatus(
            Guid inviteToken,
            int questionId,
            CancellationToken cancellationToken)
        {
            var result = await _audioIngestionService.GetAnswerStatusAsync(
                inviteToken,
                questionId,
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// GET: api/interviews/audio/{*storageKey}
        /// Stream file ghi âm phục vụ playback trên web (Admin, HR, Interviewer).
        /// Hỗ trợ HTTP 206 Partial Content để tua audio mượt mà.
        /// </summary>
        [HttpGet("audio/{*storageKey}")]
        [Authorize(Roles = "Admin,HR,Interviewer")]
        public async Task<IActionResult> StreamAudio(
            string storageKey,
            [FromServices] IAudioStorageService audioStorageService,
            CancellationToken cancellationToken)
        {
            var stream = await audioStorageService.GetAudioStreamAsync(storageKey, cancellationToken);

            var mimeType = "audio/webm";
            if (storageKey.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || storageKey.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase))
                mimeType = "audio/mp4";
            else if (storageKey.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                mimeType = "audio/wav";
            else if (storageKey.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                mimeType = "audio/mpeg";
            else if (storageKey.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                mimeType = "audio/ogg";

            return File(stream, mimeType, enableRangeProcessing: true);
        }

        /// <summary>
        /// POST: api/interviews/recovery/zombies
        /// Quét và phục hồi các tác vụ bóc băng bị kẹt hoặc quá hạn (Admin, HR).
        /// </summary>
        [HttpPost("recovery/zombies")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> RecoverZombieJobs(
            [FromServices] ITranscriptionRecoveryService recoveryService,
            CancellationToken cancellationToken)
        {
            var result = await recoveryService.RecoverZombieJobsAsync(isStartup: false, cancellationToken);
            return Ok(result);
        }
    }
}
