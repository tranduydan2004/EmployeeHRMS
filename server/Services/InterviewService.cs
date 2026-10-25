using System.Net;
using EmployeeHRMS.Api.Configuration;
using EmployeeHRMS.Api.Data;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Extensions;
using EmployeeHRMS.Api.Helpers;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Models.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interview Service — EF Core implementation (PostgreSQL).
    /// DDD model: trạng thái và AI scoring nằm trong entity.
    /// GetAllAsync: lọc dữ liệu tự động theo Role.
    /// </summary>
    public class InterviewService : IInterviewService
    {
        private readonly AppDbContext _context;
        private readonly EventLogger _eventLogger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AudioInterviewOptions _audioInterviewOptions;
        private readonly IEmailService? _emailService;
        private readonly IAudioTranscriptionQueue? _queue;
        private readonly IAudioStorageService? _audioStorageService;
        private readonly INotificationService? _notificationService;

        public InterviewService(
            AppDbContext context,
            EventLogger eventLogger,
            IHttpContextAccessor httpContextAccessor,
            IOptions<AudioInterviewOptions>? audioInterviewOptions = null,
            IEmailService? emailService = null,
            IAudioTranscriptionQueue? queue = null,
            IAudioStorageService? audioStorageService = null,
            INotificationService? notificationService = null)
        {
            _context = context;
            _eventLogger = eventLogger;
            _httpContextAccessor = httpContextAccessor;
            _audioInterviewOptions = audioInterviewOptions?.Value ?? new AudioInterviewOptions();
            _emailService = emailService;
            _queue = queue;
            _audioStorageService = audioStorageService;
            _notificationService = notificationService;
        }

        private static QuestionEvaluationResponseDto MapToEvaluationDto(QuestionEvaluation evaluation) => new()
        {
            Id = evaluation.Id,
            InterviewAnswerId = evaluation.InterviewAnswerId,
            VersionNumber = evaluation.VersionNumber,
            TranscriptSnapshot = evaluation.TranscriptSnapshot,
            Level = evaluation.Level?.ToString(),
            Score = evaluation.Score,
            Analysis = evaluation.Analysis,
            EvidenceQuotes = evaluation.EvidenceQuotes != null ? new List<string>(evaluation.EvidenceQuotes) : new List<string>(),
            Strengths = evaluation.Strengths != null ? new List<string>(evaluation.Strengths) : new List<string>(),
            Concerns = evaluation.Concerns != null ? new List<string>(evaluation.Concerns) : new List<string>(),
            ModelName = evaluation.ModelName,
            PromptVersion = evaluation.PromptVersion,
            Outcome = evaluation.Outcome.ToString(),
            Reason = evaluation.Reason,
            ErrorMessage = evaluation.ErrorMessage,
            EvaluatedAt = evaluation.EvaluatedAt,
            EvaluatedByUserId = evaluation.EvaluatedByUserId,
            IsActive = evaluation.IsActive
        };

        private static InterviewAnswerDetailDto MapToAnswerDetailDto(InterviewAnswer answer) => new()
        {
            Id = answer.Id,
            InterviewQuestionId = answer.InterviewQuestionId,
            AudioStorageKey = answer.AudioStorageKey,
            AudioDurationSeconds = answer.AudioDurationSeconds,
            SttDurationSeconds = answer.SttDurationSeconds,
            AudioMimeType = answer.AudioMimeType,
            Status = answer.Status.ToString(),
            FailedStage = answer.FailedStage?.ToString(),
            Transcript = answer.Transcript,
            EditedTranscript = answer.EditedTranscript,
            EffectiveTranscript = answer.GetEffectiveTranscript(),
            EditVersion = answer.EditVersion,
            LastEditedByUserId = answer.LastEditedByUserId,
            LastEditedAt = answer.LastEditedAt,
            DetectedLanguage = answer.DetectedLanguage,
            SpeechMetrics = answer.SpeechMetrics,
            ErrorMessage = answer.ErrorMessage,
            CreatedAt = answer.CreatedAt,
            CompletedAt = answer.CompletedAt,
            IsAudioDeleted = string.IsNullOrEmpty(answer.AudioStorageKey),
            Evaluations = answer.Evaluations?
                .OrderByDescending(e => e.VersionNumber)
                .Select(MapToEvaluationDto)
                .ToList() ?? new List<QuestionEvaluationResponseDto>()
        };

        /// <summary>
        /// Nested projection: Interview → InterviewResponseDto (kèm List<InterviewQuestionResponseDto>).
        /// </summary>
        private static InterviewResponseDto MapToResponseDto(Interview interview) => new()
        {
            Id = interview.Id,
            ApplicationId = interview.ApplicationId,
            CandidateName = interview.Application?.Candidate?.FullName ?? string.Empty,
            CandidateEmail = interview.Application?.Candidate?.Email ?? string.Empty,
            JobTitle = interview.Application?.JobPosting?.Title ?? string.Empty,
            ApplicationStatus = interview.Application?.Status.ToString() ?? string.Empty,
            InterviewerId = interview.InterviewerId,
            InterviewerName = interview.Interviewer?.Employee?.FullName ?? interview.Interviewer?.Email,
            ScheduledDate = interview.ScheduledDate,
            Status = interview.Status.ToString(),
            AiOverallScore = interview.AiOverallScore,
            AiOverallSummary = interview.AiOverallSummary,
            Recommendation = interview.Recommendation?.ToString(),
            IsPersonalDataErased = (interview.Answers != null && interview.Answers.Any()
                ? interview.Answers
                : interview.Questions.Where(q => q.Answer != null).Select(q => q.Answer!))
                .Any() &&
                (interview.Answers != null && interview.Answers.Any()
                ? interview.Answers
                : interview.Questions.Where(q => q.Answer != null).Select(q => q.Answer!))
                .All(a => string.IsNullOrEmpty(a.AudioStorageKey) && a.Transcript == null),

            // Nested projection: List<InterviewQuestion> → List<InterviewQuestionResponseDto>
            Questions = interview.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new InterviewQuestionResponseDto
                {
                    Id = q.Id,
                    Question = q.Question,
                    CandidateAnswer = q.CandidateAnswer,
                    AiQuestionScore = q.AiQuestionScore,
                    AiFeedback = q.AiFeedback,
                    OrderIndex = q.OrderIndex,
                    Category = q.Category?.ToString(),
                    ScoringRubric = q.ScoringRubric,
                    Answer = q.Answer != null ? MapToAnswerDetailDto(q.Answer) : null
                })
                .ToList()
        };

        private static readonly Dictionary<string, Func<IQueryable<Interview>, bool, IOrderedQueryable<Interview>>> SortMappings =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["scheduleddate"] = (q, desc) => desc ? q.OrderByDescending(i => i.ScheduledDate) : q.OrderBy(i => i.ScheduledDate),
                ["status"] = (q, desc) => desc ? q.OrderByDescending(i => i.Status) : q.OrderBy(i => i.Status),
                ["id"] = (q, desc) => desc ? q.OrderByDescending(i => i.Id) : q.OrderBy(i => i.Id),
            };

        public async Task<PagedResult<InterviewResponseDto>> GetAllAsync(PaginationParams paginationParams)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var role = user?.GetRole();
            var currentUserId = user?.GetUserId();

            IQueryable<Interview> query = _context.Interviews
                .AsNoTracking()
                .Include(i => i.Questions)
                    .ThenInclude(q => q.Answer)
                        .ThenInclude(a => a!.Evaluations)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee);

            // 1) Filter theo Role trước
            if (role == nameof(UserRole.Interviewer) && currentUserId.HasValue)
            {
                query = query.Where(i => i.InterviewerId == currentUserId.Value);
            }
            else if (role is not (nameof(UserRole.Admin) or nameof(UserRole.HR)))
            {
                // Candidate hoặc vai trò khác không có quyền xem dashboard interviews
                return new PagedResult<InterviewResponseDto>
                {
                    Items = new List<InterviewResponseDto>(),
                    TotalCount = 0,
                    PageNumber = paginationParams.PageNumber,
                    PageSize = paginationParams.PageSize
                };
            }
            // Admin, HR: no filter — return all

            // 2) ILike Search (nếu có) trên tập đã qua bước 1
            if (!string.IsNullOrWhiteSpace(paginationParams.Search))
            {
                var search = paginationParams.Search.Trim();
                if (_context.Database.IsNpgsql())
                {
                    query = query.Where(i =>
                        EF.Functions.ILike(i.Application.JobPosting.Title, $"%{search}%") ||
                        EF.Functions.ILike(i.Application.Candidate.FullName, $"%{search}%") ||
                        EF.Functions.ILike(i.Application.Candidate.Email, $"%{search}%"));
                }
                else
                {
                    query = query.Where(i =>
                        i.Application.JobPosting.Title.ToLower().Contains(search.ToLower()) ||
                        i.Application.Candidate.FullName.ToLower().Contains(search.ToLower()) ||
                        i.Application.Candidate.Email.ToLower().Contains(search.ToLower()));
                }
            }

            // 3) CountAsync() trên tập đã filter + search
            var totalCount = await query.CountAsync();

            // 4) Whitelist Sorting ("scheduleddate", "status", "id", default "scheduleddate" desc)
            IOrderedQueryable<Interview> orderedQuery;
            if (!string.IsNullOrWhiteSpace(paginationParams.SortBy) &&
                SortMappings.TryGetValue(paginationParams.SortBy, out var sortFunc))
            {
                orderedQuery = sortFunc(query, paginationParams.IsDescending);
            }
            else
            {
                orderedQuery = paginationParams.IsDescending
                    ? query.OrderByDescending(i => i.ScheduledDate)
                    : query.OrderByDescending(i => i.ScheduledDate);
            }

            // 5) Skip/Take -> Select -> ToListAsync()
            var interviews = await orderedQuery
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PagedResult<InterviewResponseDto>
            {
                Items = interviews.Select(MapToResponseDto).ToList(),
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }

        public async Task<InterviewResponseDto?> GetByIdAsync(int id)
        {
            var interview = await _context.Interviews
                .AsNoTracking()
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Application)
                    .ThenInclude(a => a.JobPosting)
                .Include(i => i.Questions)
                    .ThenInclude(q => q.Answer)
                        .ThenInclude(a => a!.Evaluations)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee)
                .FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new NotFoundException("Interview", id);

            return MapToResponseDto(interview);
        }

        public async Task<IEnumerable<InterviewResponseDto>> GetByApplicationAsync(int applicationId)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var role = user?.GetRole();
            var currentUserId = user?.GetUserId();

            IQueryable<Interview> query = _context.Interviews
                .AsNoTracking()
                .Include(i => i.Questions)
                    .ThenInclude(q => q.Answer)
                        .ThenInclude(a => a!.Evaluations)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee)
                .Where(i => i.ApplicationId == applicationId);

            if (role == nameof(UserRole.Interviewer) && currentUserId.HasValue)
            {
                query = query.Where(i => i.InterviewerId == currentUserId.Value);
            }
            else if (role is not (nameof(UserRole.Admin) or nameof(UserRole.HR)))
            {
                return Enumerable.Empty<InterviewResponseDto>();
            }

            var interviews = await query.ToListAsync();
            return interviews.Select(MapToResponseDto);
        }

        public async Task<InterviewResponseDto> CreateAsync(InterviewCreateDto dto)
        {
            // Kiểm tra ApplicationId tồn tại (ràng buộc FK)
            var applicationExists = await _context.Applications.AnyAsync(a => a.Id == dto.ApplicationId);
            if (!applicationExists)
                throw new BusinessRuleException($"Application with ID {dto.ApplicationId} does not exist.");

            // Kiểm tra InterviewerId tồn tại nếu được cung cấp
            if (dto.InterviewerId.HasValue)
            {
                var interviewerExists = await _context.Users.AnyAsync(u =>
                    u.Id == dto.InterviewerId.Value &&
                    u.Role == UserRole.Interviewer);
                if (!interviewerExists)
                    throw new BusinessRuleException($"Interviewer with ID {dto.InterviewerId.Value} does not exist or is not an Interviewer.");
            }

            var interview = new Interview
            {
                ApplicationId = dto.ApplicationId,
                ScheduledDate = dto.ScheduledDate,
                InterviewerId = dto.InterviewerId
            };
            interview.Schedule(); // DDD domain method — set Status = Scheduled

            await _context.Interviews.AddAsync(interview);
            await _context.SaveChangesAsync();

            if (interview.InterviewerId.HasValue)
            {
                await _context.Entry(interview)
                    .Reference(i => i.Interviewer)
                    .Query()
                    .Include(u => u.Employee)
                    .LoadAsync();
            }

            _eventLogger.LogEvent("Interview",
                $"created (ApplicationId: {dto.ApplicationId}, Date: {dto.ScheduledDate:yyyy-MM-dd})",
                _eventLogger.OnEntityCreated);

            return MapToResponseDto(interview);
        }

        /// <summary>
        /// Thêm câu hỏi vào buổi phỏng vấn.
        /// Sau khi thêm, gọi DDD domain method RecalculateOverallScore() để tính lại điểm.
        /// </summary>
        public async Task<bool> AddQuestionAsync(int interviewId, InterviewQuestionCreateDto dto)
        {
            var interview = await _context.Interviews
                .Include(i => i.Questions)
                .FirstOrDefaultAsync(i => i.Id == interviewId);

            if (interview == null) throw new NotFoundException("Interview", interviewId);

            var question = new InterviewQuestion
            {
                InterviewId = interviewId,
                Question = dto.Question,
                OrderIndex = dto.OrderIndex,
                ThinkSeconds = _audioInterviewOptions.DefaultThinkSeconds,
                AnswerSeconds = _audioInterviewOptions.DefaultAnswerSeconds
            };

            interview.Questions.Add(question);

            // DDD domain method — tính lại AiOverallScore = trung bình điểm các câu
            interview.RecalculateOverallScore();

            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("InterviewQuestion",
                $"added to Interview {interviewId} (Question: {dto.Question[..Math.Min(50, dto.Question.Length)]}...)",
                _eventLogger.OnEntityCreated);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var interview = await _context.Interviews.FindAsync(id)
                ?? throw new NotFoundException("Interview", id);

            _context.Interviews.Remove(interview);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Interview", $"deleted (Id: {id})", _eventLogger.OnEntityDeleted);
            return true;
        }

        public async Task<InterviewResponseDto?> CompleteInterviewAsync(int id, string? summary = null)
        {
            var interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee)
                .Include(i => i.Questions)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview == null) return null;

            interview.Complete(summary);
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("Interview", $"Interview #{id} marked as Completed", _eventLogger.OnEntityUpdated);

            return MapToResponseDto(interview);
        }

        /// <summary>
        /// Fetch Interview entity cho authorization handler (không DTO).
        /// </summary>
        public async Task<Interview?> GetEntityByIdAsync(int id)
        {
            return await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Application)
                    .ThenInclude(a => a.JobPosting)
                .Include(i => i.Interviewer)
                    .ThenInclude(u => u!.Employee)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        /// <summary>
        /// Fetch entity kèm Questions và Candidate phục vụ đánh giá/trả lời câu hỏi.
        /// </summary>
        public async Task<Interview?> GetEntityWithQuestionsAsync(int id)
        {
            return await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Questions)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        /// <summary>
        /// Cập nhật câu trả lời của ứng viên cho câu hỏi phỏng vấn.
        /// </summary>
        public async Task<bool> UpdateQuestionAnswerAsync(InterviewQuestion question, string candidateAnswer)
        {
            question.CandidateAnswer = candidateAnswer;
            await _context.SaveChangesAsync();

            _eventLogger.LogEvent("InterviewQuestion",
                $"Candidate answer updated for Question #{question.Id} in Interview #{question.InterviewId}",
                _eventLogger.OnEntityUpdated);

            return true;
        }

        // ============================================================
        // Candidate Audio Interview (Phase 2)
        // ============================================================

        public async Task<InterviewInviteResponseDto> CreateInviteAsync(
            int applicationId,
            InterviewInviteCreateDto? dto = null,
            CancellationToken cancellationToken = default)
        {
            var application = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.JobPosting)
                    .ThenInclude(j => j.QuestionBankItems)
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
                ?? throw new NotFoundException("Application", applicationId);

            var job = application.JobPosting
                ?? throw new BusinessRuleException("Không tìm thấy thông tin tin tuyển dụng gắn với hồ sơ ứng tuyển này.");

            if (job.Status != JobPostingStatus.Approved && job.Status != JobPostingStatus.Published)
            {
                throw new BusinessRuleException($"Chỉ có thể tạo phiên phỏng vấn khi tin tuyển dụng đã được duyệt (Approved hoặc Published). Trạng thái hiện tại: {job.Status}.");
            }

            var qbItems = job.QuestionBankItems
                .OrderBy(q => q.OrderIndex)
                .ToList();

            if (!qbItems.Any())
            {
                throw new BusinessRuleException("Tin tuyển dụng chưa có câu hỏi phỏng vấn trong Question Bank. Vui lòng tạo câu hỏi trước khi mời phỏng vấn.");
            }

            // Chuyển trạng thái Application theo state machine hợp lệ: sơ loại audio thuộc giai đoạn Screening
            if (application.Status == ApplicationStatus.Applied)
            {
                application.TransitionTo(ApplicationStatus.Screening);
            }
            else if (application.Status != ApplicationStatus.Screening)
            {
                throw new BusinessRuleException($"Không thể mời phỏng vấn hồ sơ đang ở trạng thái '{application.Status}'. Hồ sơ phải ở trạng thái Applied hoặc Screening.");
            }

            var validityDays = dto?.ValidityDays ?? _audioInterviewOptions.DefaultInviteExpiryDays;
            if (validityDays <= 0) validityDays = 3;

            var interview = new Interview
            {
                ApplicationId = application.Id
            };
            interview.Invite(TimeSpan.FromDays(validityDays));

            // Copy câu hỏi từ QuestionBankItem sang InterviewQuestion: clone từng field của ScoringRubric (không gán reference)
            foreach (var qb in qbItems)
            {
                // D3: Khi tạo lời mời, nếu câu hỏi chưa có rubric đầy đủ thì trả lỗi rõ ràng cho HR thay vì để đến lúc chấm mới NotScorable
                if (qb.ScoringRubric == null ||
                    string.IsNullOrWhiteSpace(qb.ScoringRubric.Poor) ||
                    string.IsNullOrWhiteSpace(qb.ScoringRubric.Acceptable) ||
                    string.IsNullOrWhiteSpace(qb.ScoringRubric.Good) ||
                    string.IsNullOrWhiteSpace(qb.ScoringRubric.Excellent))
                {
                    throw new BusinessRuleException(
                        $"Câu hỏi #{qb.Id} ('{qb.Question}') chưa có đầy đủ Scoring Rubric 4 mức. Vui lòng hoàn thiện rubric cho câu hỏi trước khi tạo lời mời phỏng vấn.");
                }

                interview.Questions.Add(new InterviewQuestion
                {
                    Question = qb.Question,
                    OrderIndex = qb.OrderIndex,
                    Category = qb.Category,
                    ThinkSeconds = qb.ThinkSeconds ?? _audioInterviewOptions.DefaultThinkSeconds,
                    AnswerSeconds = qb.AnswerSeconds ?? _audioInterviewOptions.DefaultAnswerSeconds,
                    ScoringRubric = new ScoringRubric
                    {
                        Excellent = qb.ScoringRubric.Excellent,
                        Good = qb.ScoringRubric.Good,
                        Acceptable = qb.ScoringRubric.Acceptable,
                        Poor = qb.ScoringRubric.Poor
                    }
                });
            }

            await _context.Interviews.AddAsync(interview, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var portalBase = _audioInterviewOptions.BaseCandidatePortalUrl.TrimEnd('/');
            var inviteUrl = $"{portalBase}/{interview.InviteToken}";

            _eventLogger.LogEvent("Interview",
                $"invited (ApplicationId: {application.Id}, Token: {interview.InviteToken}, ExpiresAt: {interview.InviteExpiresAt:yyyy-MM-dd HH:mm})",
                _eventLogger.OnEntityCreated);

            // Gửi email mời phỏng vấn (tùy chọn qua MailKit)
            if (dto?.SendEmail != false && !string.IsNullOrWhiteSpace(application.Candidate?.Email) && _emailService != null)
            {
                var candidateName = application.Candidate.FullName;
                var subject = $"[HRMS] Lời mời tham gia phỏng vấn vị trí {job.Title}";
                var body = $@"<p>Xin chào <strong>{candidateName}</strong>,</p>
<p>Bạn đã được mời tham gia vòng phỏng vấn ghi âm sơ loại cho vị trí <strong>{job.Title}</strong>.</p>
<p>Vui lòng truy cập liên kết dưới đây để bắt đầu bài phỏng vấn:</p>
<p><a href='{inviteUrl}' style='display:inline-block;padding:10px 20px;background-color:#2563eb;color:#ffffff;text-decoration:none;border-radius:6px;'>Bắt đầu phỏng vấn</a></p>
<p>Liên kết sẽ hết hạn vào lúc: <strong>{interview.InviteExpiresAt:dd/MM/yyyy HH:mm} (UTC)</strong>.</p>
<p>Trân trọng,<br/>Phòng Tuyển dụng</p>";

                try
                {
                    await _emailService.SendEmailAsync(application.Candidate.Email, subject, body);
                }
                catch
                {
                    // Email gửi lỗi không chặn luồng chính
                }
            }

            return new InterviewInviteResponseDto
            {
                InterviewId = interview.Id,
                ApplicationId = application.Id,
                InviteToken = interview.InviteToken,
                InviteUrl = inviteUrl,
                InviteExpiresAt = interview.InviteExpiresAt!.Value,
                ScheduledDate = interview.ScheduledDate,
                TotalQuestions = interview.Questions.Count,
                Status = interview.Status.ToString(),
                CandidateName = application.Candidate?.FullName ?? string.Empty,
                CandidateEmail = application.Candidate?.Email ?? string.Empty,
                JobTitle = job.Title
            };
        }

        public async Task<CandidateInterviewSessionResponseDto> GetCandidateSessionAsync(
            Guid inviteToken,
            CancellationToken cancellationToken = default)
        {
            var interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Application)
                    .ThenInclude(a => a.JobPosting)
                .Include(i => i.Questions)
                .Include(i => i.Answers)
                .FirstOrDefaultAsync(i => i.InviteToken == inviteToken, cancellationToken);

            if (interview == null)
            {
                throw new NotFoundException("Không tìm thấy phiên phỏng vấn với liên kết đã cung cấp.");
            }

            if (interview.Status == InterviewStatus.Cancelled)
            {
                throw new BusinessRuleException("Phiên phỏng vấn này đã bị hủy.", HttpStatusCode.BadRequest);
            }

            // Kiểm tra hết hạn: nếu quá hạn thì cập nhật lazy và trả 410 Gone
            if (interview.IsInviteExpired(DateTime.UtcNow) || interview.Status == InterviewStatus.Expired)
            {
                if (interview.Status != InterviewStatus.Expired)
                {
                    interview.MarkExpired();
                    await _context.SaveChangesAsync(cancellationToken);
                }
                throw new BusinessRuleException("Liên kết phỏng vấn này đã hết hạn.", HttpStatusCode.Gone);
            }

            // Đã hoàn thành: trả 409 Conflict
            if (interview.Status == InterviewStatus.Completed)
            {
                throw new ConflictException("Phiên phỏng vấn này đã hoàn thành.");
            }

            var answeredQuestionIds = interview.Answers
                .Select(a => a.InterviewQuestionId)
                .ToHashSet();

            // TUYỆT ĐỐI không trả ScoringRubric hay field nội bộ nào khác
            return new CandidateInterviewSessionResponseDto
            {
                InviteToken = interview.InviteToken,
                JobTitle = interview.Application?.JobPosting?.Title ?? "Phỏng vấn",
                CandidateName = interview.Application?.Candidate?.FullName ?? "Ứng viên",
                Status = interview.Status.ToString(),
                InviteExpiresAt = interview.InviteExpiresAt,
                ConsentedAt = interview.ConsentedAt,
                TotalQuestions = interview.Questions.Count,
                Questions = interview.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new CandidateQuestionDto
                    {
                        Id = q.Id,
                        Question = q.Question,
                        OrderIndex = q.OrderIndex,
                        ThinkSeconds = q.ThinkSeconds,
                        AnswerSeconds = q.AnswerSeconds,
                        HasAnswer = answeredQuestionIds.Contains(q.Id)
                    })
                    .ToList()
            };
        }

        public async Task<CandidateInterviewStartResponseDto> StartCandidateInterviewAsync(
            Guid inviteToken,
            CancellationToken cancellationToken = default)
        {
            var interview = await _context.Interviews
                .FirstOrDefaultAsync(i => i.InviteToken == inviteToken, cancellationToken);

            if (interview == null)
            {
                throw new NotFoundException("Không tìm thấy phiên phỏng vấn với liên kết đã cung cấp.");
            }

            if (interview.Status == InterviewStatus.Cancelled)
            {
                throw new BusinessRuleException("Phiên phỏng vấn này đã bị hủy.", HttpStatusCode.BadRequest);
            }

            if (interview.IsInviteExpired(DateTime.UtcNow) || interview.Status == InterviewStatus.Expired)
            {
                if (interview.Status != InterviewStatus.Expired)
                {
                    interview.MarkExpired();
                    await _context.SaveChangesAsync(cancellationToken);
                }
                throw new BusinessRuleException("Liên kết phỏng vấn này đã hết hạn.", HttpStatusCode.Gone);
            }

            if (interview.Status == InterviewStatus.Completed)
            {
                throw new ConflictException("Phiên phỏng vấn này đã hoàn thành.");
            }

            // Idempotent: gọi lại khi đã InProgress sẽ giữ nguyên ConsentedAt
            interview.MarkInProgress();
            await _context.SaveChangesAsync(cancellationToken);

            _eventLogger.LogEvent("Interview",
                $"started (InterviewId: {interview.Id}, Token: {interview.InviteToken}, ConsentedAt: {interview.ConsentedAt:yyyy-MM-dd HH:mm})",
                _eventLogger.OnEntityUpdated);

            return new CandidateInterviewStartResponseDto
            {
                InviteToken = interview.InviteToken,
                Status = interview.Status.ToString(),
                ConsentedAt = interview.ConsentedAt,
                Message = "Bắt đầu phiên phỏng vấn thành công."
            };
        }

        // ============================================================
        // AI Rubric Grading & Re-evaluation Pipeline (Phase 4)
        // ============================================================

        public async Task<InterviewAnswerDetailDto> UpdateTranscriptAsync(
            int interviewId,
            int questionId,
            string editedTranscript,
            int currentUserId,
            int? expectedVersion = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(editedTranscript))
            {
                throw new BusinessRuleException("Nội dung bản bóc băng chỉnh sửa không được để trống.");
            }

            var interview = await _context.Interviews
                .Include(i => i.Questions)
                    .ThenInclude(q => q.Answer)
                        .ThenInclude(a => a!.Evaluations)
                .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);

            if (interview == null)
                throw new NotFoundException("Interview", interviewId);

            var question = interview.Questions.FirstOrDefault(q => q.Id == questionId);
            if (question == null)
                throw new NotFoundException($"Không tìm thấy câu hỏi #{questionId} trong buổi phỏng vấn #{interviewId}.");

            var answer = question.Answer;
            if (answer == null)
                throw new NotFoundException($"Câu trả lời cho câu hỏi #{questionId} chưa tồn tại.");

            // Optimistic Concurrency Check: phát hiện xung đột khi hai người cùng sửa transcript
            if (expectedVersion.HasValue && answer.EditVersion != expectedVersion.Value)
            {
                throw new ConflictException(
                    $"Bản bóc băng đã được cập nhật bởi người khác (phiên bản hiện tại trên hệ thống: v{answer.EditVersion}). Vui lòng tải lại trước khi lưu.");
            }

            if (string.IsNullOrEmpty(answer.AudioStorageKey) && answer.Transcript == null && answer.EditedTranscript == null)
            {
                throw new BusinessRuleException("Dữ liệu cá nhân của câu trả lời này đã bị xóa, không thể chỉnh sửa transcript.");
            }

            if (answer.Status == AudioProcessingStatus.Pending || answer.Status == AudioProcessingStatus.Transcribing)
            {
                throw new BusinessRuleException("Đoạn audio đang trong quá trình bóc băng, chưa thể chỉnh sửa bản bóc băng.");
            }

            if (answer.Status == AudioProcessingStatus.Scoring)
            {
                throw new ConflictException("Đang trong tiến trình chấm điểm, không thể chỉnh sửa transcript.");
            }

            answer.UpdateTranscript(editedTranscript, currentUserId);
            await _context.SaveChangesAsync(cancellationToken);

            _eventLogger.LogEvent("InterviewAnswer",
                $"updated transcript (AnswerId: {answer.Id}, QuestionId: {questionId}, UserId: {currentUserId}, NewVersion: {answer.EditVersion})",
                _eventLogger.OnEntityUpdated);

            return MapToAnswerDetailDto(answer);
        }

        public async Task<ReevaluateResponseDto> ReevaluateAnswerAsync(
            int interviewId,
            int questionId,
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            var interview = await _context.Interviews
                .Include(i => i.Questions)
                    .ThenInclude(q => q.Answer)
                        .ThenInclude(a => a!.Evaluations)
                .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);

            if (interview == null)
                throw new NotFoundException("Interview", interviewId);

            var question = interview.Questions.FirstOrDefault(q => q.Id == questionId);
            if (question == null)
                throw new NotFoundException($"Không tìm thấy câu hỏi #{questionId} trong buổi phỏng vấn #{interviewId}.");

            var answer = question.Answer;
            if (answer == null)
                throw new NotFoundException($"Câu trả lời cho câu hỏi #{questionId} chưa tồn tại.");

            // Tránh chạy đè hai lần re-evaluate đồng thời (Q3: State machine concurrency guard)
            if (answer.Status == AudioProcessingStatus.Scoring || answer.Status == AudioProcessingStatus.Transcribing)
            {
                throw new ConflictException("Câu trả lời đang trong tiến trình xử lý, không thể re-evaluate chồng chéo.");
            }

            // Chỉ cho phép Re-evaluate khi Answer đã hoàn thành (Completed) hoặc bị lỗi (Failed)
            if (answer.Status != AudioProcessingStatus.Completed && answer.Status != AudioProcessingStatus.Failed)
            {
                throw new BusinessRuleException($"Không thể re-evaluate câu trả lời ở trạng thái '{answer.Status}'.");
            }

            var effectiveTranscript = answer.GetEffectiveTranscript();
            if (string.IsNullOrWhiteSpace(effectiveTranscript))
            {
                throw new BusinessRuleException("Không thể re-evaluate vì chưa có nội dung bóc băng.");
            }

            answer.StartScoring();
            await _context.SaveChangesAsync(cancellationToken);

            if (_queue != null)
            {
                await _queue.EnqueueAsync(new AudioTranscriptionJob
                {
                    InterviewAnswerId = answer.Id,
                    EnqueuedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            _eventLogger.LogEvent("InterviewAnswer",
                $"re-evaluation initiated (AnswerId: {answer.Id}, QuestionId: {questionId}, UserId: {currentUserId})",
                _eventLogger.OnEntityUpdated);

            return new ReevaluateResponseDto
            {
                InterviewAnswerId = answer.Id,
                QuestionId = questionId,
                Status = answer.Status.ToString(),
                Message = "Tác vụ chấm lại đã được đưa vào hàng đợi xử lý.",
                EnqueuedAt = DateTime.UtcNow
            };
        }

        public async Task<List<QuestionEvaluationResponseDto>> GetInterviewEvaluationsAsync(
            int interviewId,
            CancellationToken cancellationToken = default)
        {
            var exists = await _context.Interviews.AnyAsync(i => i.Id == interviewId, cancellationToken);
            if (!exists)
                throw new NotFoundException("Interview", interviewId);

            var evaluations = await _context.QuestionEvaluations
                .AsNoTracking()
                .Include(e => e.InterviewAnswer)
                    .ThenInclude(a => a.InterviewQuestion)
                .Where(e => e.InterviewAnswer.InterviewQuestion.InterviewId == interviewId)
                .OrderBy(e => e.InterviewAnswer.InterviewQuestion.OrderIndex)
                .ThenByDescending(e => e.VersionNumber)
                .ToListAsync(cancellationToken);

            return evaluations.Select(MapToEvaluationDto).ToList();
        }

        public async Task<List<QuestionEvaluationResponseDto>> GetQuestionEvaluationsAsync(
            int interviewId,
            int questionId,
            CancellationToken cancellationToken = default)
        {
            var exists = await _context.InterviewQuestions
                .AnyAsync(q => q.InterviewId == interviewId && q.Id == questionId, cancellationToken);
            if (!exists)
                throw new NotFoundException($"Không tìm thấy câu hỏi #{questionId} trong buổi phỏng vấn #{interviewId}.");

            var evaluations = await _context.QuestionEvaluations
                .AsNoTracking()
                .Where(e => e.InterviewAnswer.InterviewQuestionId == questionId)
                .OrderByDescending(e => e.VersionNumber)
                .ToListAsync(cancellationToken);

            return evaluations.Select(MapToEvaluationDto).ToList();
        }

        // ============================================================
        // HR/Interviewer Review & Governance (Phase 5)
        // ============================================================

        public async Task<InterviewResponseDto> AssignInterviewerAsync(
            int interviewId,
            int interviewerId,
            CancellationToken cancellationToken = default)
        {
            var interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Questions)
                    .ThenInclude(q => q.Answer)
                        .ThenInclude(a => a!.Evaluations)
                .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken)
                ?? throw new NotFoundException("Interview", interviewId);

            var interviewer = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == interviewerId && u.Role == UserRole.Interviewer, cancellationToken);

            if (interviewer == null)
            {
                throw new BusinessRuleException($"Interviewer with ID {interviewerId} does not exist or is not an Interviewer.");
            }

            interview.AssignInterviewer(interviewerId);
            await _context.SaveChangesAsync(cancellationToken);

            _eventLogger.LogEvent("Interview",
                $"Assigned Interviewer #{interviewerId} to Interview #{interviewId}",
                _eventLogger.OnEntityUpdated);

            if (_notificationService != null)
            {
                var candidateName = interview.Application?.Candidate?.FullName ?? "Ứng viên";
                await _notificationService.SendToUserAsync(
                    interviewerId,
                    "Phân công phỏng vấn",
                    "INTERVIEW_ASSIGNED_INTERVIEWER",
                    new Dictionary<string, string>
                    {
                        { "candidateName", candidateName },
                        { "interviewId", interview.Id.ToString() }
                    },
                    NotificationType.ActionRequired,
                    relatedEntity: "Interview",
                    relatedEntityId: interview.Id,
                    fallbackMessage: $"Bạn được phân công phỏng vấn/đánh giá ứng viên {candidateName} (Buổi phỏng vấn #{interview.Id}).");
            }

            return MapToResponseDto(interview);
        }

        public async Task<ScreeningDecisionResponseDto> CreateScreeningDecisionAsync(
            int interviewId,
            ScreeningDecisionCreateDto dto,
            int currentUserId,
            string currentRole,
            CancellationToken cancellationToken = default)
        {
            var interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Application)
                    .ThenInclude(a => a.JobPosting)
                .Include(i => i.Questions)
                    .ThenInclude(q => q.Answer)
                        .ThenInclude(a => a!.Evaluations)
                .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken)
                ?? throw new NotFoundException("Interview", interviewId);

            var application = interview.Application
                ?? throw new BusinessRuleException("Không tìm thấy thông tin đơn ứng tuyển cho buổi phỏng vấn này.");

            var systemRecommendation = interview.Recommendation?.ToString() ?? "PendingReview";
            var systemOverallScore = interview.AiOverallScore;

            var snapshot = new List<QuestionEvaluationSnapshotItem>();
            var scoredCount = 0;
            var totalCount = interview.Questions.Count;

            foreach (var q in interview.Questions.OrderBy(x => x.OrderIndex))
            {
                var activeEval = q.Answer?.Evaluations?.FirstOrDefault(e => e.IsActive);
                if (activeEval != null) scoredCount++;

                snapshot.Add(new QuestionEvaluationSnapshotItem
                {
                    InterviewQuestionId = q.Id,
                    EvaluationId = activeEval?.Id,
                    VersionNumber = activeEval?.VersionNumber,
                    Level = activeEval?.Level?.ToString(),
                    Score = activeEval?.Score,
                    Outcome = activeEval?.Outcome.ToString()
                });
            }

            var decision = ScreeningDecision.Create(
                applicationId: application.Id,
                interviewId: interview.Id,
                decidedByUserId: currentUserId,
                decidedByRole: currentRole,
                decisionType: dto.DecisionType,
                systemRecommendation: systemRecommendation,
                systemOverallScore: systemOverallScore,
                evaluationSnapshot: snapshot,
                scoredQuestionCount: scoredCount,
                totalQuestionCount: totalCount,
                overrideReason: dto.OverrideReason,
                notes: dto.Notes);

            // Cập nhật trạng thái Application theo quyết định sơ loại của con người
            if (dto.DecisionType == ScreeningDecisionType.AdvanceToInDepth)
            {
                // Vượt qua sơ loại -> chuyển sang phỏng vấn chuyên sâu
                if (application.Status == ApplicationStatus.Screening)
                {
                    application.TransitionTo(ApplicationStatus.Interviewing);
                }
            }
            else if (dto.DecisionType == ScreeningDecisionType.Reject)
            {
                // Từ chối hồ sơ
                if (application.Status != ApplicationStatus.Rejected && application.Status != ApplicationStatus.Offered)
                {
                    application.TransitionTo(ApplicationStatus.Rejected);
                }
            }
            // Nếu HoldForReview: giữ nguyên trạng thái Screening

            // Đánh dấu Interview là Completed nếu đang InProgress hoặc Expired
            if (interview.Status == InterviewStatus.InProgress || interview.Status == InterviewStatus.Expired)
            {
                interview.Complete(summary: dto.Notes);
            }

            _context.ScreeningDecisions.Add(decision);
            await _context.SaveChangesAsync(cancellationToken);

            _eventLogger.LogEvent("ScreeningDecision",
                $"Decision recorded for Interview #{interviewId} (Type: {dto.DecisionType}, DecidedBy: {currentUserId})",
                _eventLogger.OnEntityCreated);

            // Gửi thông báo cho Interviewer và Ứng viên nếu có cấu hình NotificationService
            if (_notificationService != null)
            {
                var candidateName = application.Candidate?.FullName ?? "Ứng viên";
                var jobTitle = application.JobPosting?.Title ?? "Vị trí ứng tuyển";
                var decisionLabel = dto.DecisionType switch
                {
                    ScreeningDecisionType.AdvanceToInDepth => "Chuyển tiếp phỏng vấn chuyên sâu",
                    ScreeningDecisionType.Reject => "Từ chối",
                    ScreeningDecisionType.HoldForReview => "Tạm hoãn xem xét thêm",
                    _ => dto.DecisionType.ToString()
                };

                // 1. Gửi cho Interviewer được phân công (nếu có và không phải chính người vừa ra quyết định)
                if (interview.InterviewerId.HasValue && interview.InterviewerId.Value != currentUserId)
                {
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            interview.InterviewerId.Value,
                            "Quyết định sơ loại mới",
                            "SCREENING_DECISION_RECORDED",
                            new Dictionary<string, string>
                            {
                                { "candidateName", candidateName },
                                { "interviewId", interview.Id.ToString() },
                                { "decisionType", decisionLabel }
                            },
                            NotificationType.Info,
                            relatedEntity: "Interview",
                            relatedEntityId: interview.Id,
                            fallbackMessage: $"Quyết định sơ loại cho ứng viên {candidateName} (Phỏng vấn #{interview.Id}) đã được ghi nhận: {decisionLabel}.");
                    }
                    catch
                    {
                        // Lỗi gửi notification không ảnh hưởng đến quyết định đã lưu
                    }
                }

                // 2. Gửi cho Ứng viên nếu ứng viên có tài khoản hệ thống (Candidate.UserId) khi trạng thái thay đổi
                if (application.Candidate?.UserId.HasValue == true &&
                    (dto.DecisionType == ScreeningDecisionType.AdvanceToInDepth || dto.DecisionType == ScreeningDecisionType.Reject))
                {
                    var statusLabel = dto.DecisionType == ScreeningDecisionType.AdvanceToInDepth
                        ? "Phỏng vấn chuyên sâu"
                        : "Đã từ chối";

                    try
                    {
                        await _notificationService.SendToUserAsync(
                            application.Candidate.UserId.Value,
                            "Cập nhật trạng thái ứng tuyển",
                            "APPLICATION_STATUS_CHANGED",
                            new Dictionary<string, string>
                            {
                                { "jobTitle", jobTitle },
                                { "status", statusLabel }
                            },
                            dto.DecisionType == ScreeningDecisionType.AdvanceToInDepth ? NotificationType.Success : NotificationType.Warning,
                            relatedEntity: "Application",
                            relatedEntityId: application.Id,
                            fallbackMessage: $"Hồ sơ ứng tuyển của bạn cho vị trí {jobTitle} đã được cập nhật sang trạng thái: {statusLabel}.");
                    }
                    catch
                    {
                        // Lỗi gửi notification không ảnh hưởng đến quyết định đã lưu
                    }
                }
            }

            // Fetch user name for response
            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
            var userName = user?.Employee?.FullName ?? user?.Email;

            return new ScreeningDecisionResponseDto
            {
                Id = decision.Id,
                ApplicationId = decision.ApplicationId,
                InterviewId = decision.InterviewId,
                DecidedByUserId = decision.DecidedByUserId,
                DecidedByUserName = userName,
                DecidedByRole = decision.DecidedByRole,
                DecidedAt = decision.DecidedAt,
                DecisionType = decision.DecisionType.ToString(),
                SystemRecommendation = decision.SystemRecommendation,
                SystemOverallScore = decision.SystemOverallScore,
                EvaluationSnapshot = decision.EvaluationSnapshot,
                ScoredQuestionCount = decision.ScoredQuestionCount,
                TotalQuestionCount = decision.TotalQuestionCount,
                OverrideReason = decision.OverrideReason,
                Notes = decision.Notes
            };
        }

        public async Task<IEnumerable<ScreeningDecisionResponseDto>> GetScreeningDecisionsAsync(
            int interviewId,
            CancellationToken cancellationToken = default)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var role = user?.GetRole();
            var currentUserId = user?.GetUserId();

            var interview = await _context.Interviews
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken)
                ?? throw new NotFoundException("Interview", interviewId);

            if (role == nameof(UserRole.Interviewer) && currentUserId.HasValue && interview.InterviewerId != currentUserId.Value)
            {
                return Enumerable.Empty<ScreeningDecisionResponseDto>();
            }
            else if (role is not (nameof(UserRole.Admin) or nameof(UserRole.HR) or nameof(UserRole.Interviewer)))
            {
                return Enumerable.Empty<ScreeningDecisionResponseDto>();
            }

            var decisions = await _context.ScreeningDecisions
                .AsNoTracking()
                .Include(d => d.DecidedByUser)
                    .ThenInclude(u => u.Employee)
                .Where(d => d.InterviewId == interviewId)
                .OrderByDescending(d => d.DecidedAt)
                .ToListAsync(cancellationToken);

            return decisions.Select(d => new ScreeningDecisionResponseDto
            {
                Id = d.Id,
                ApplicationId = d.ApplicationId,
                InterviewId = d.InterviewId,
                DecidedByUserId = d.DecidedByUserId,
                DecidedByUserName = d.DecidedByUser?.Employee?.FullName ?? d.DecidedByUser?.Email,
                DecidedByRole = d.DecidedByRole,
                DecidedAt = d.DecidedAt,
                DecisionType = d.DecisionType.ToString(),
                SystemRecommendation = d.SystemRecommendation,
                SystemOverallScore = d.SystemOverallScore,
                EvaluationSnapshot = d.EvaluationSnapshot,
                ScoredQuestionCount = d.ScoredQuestionCount,
                TotalQuestionCount = d.TotalQuestionCount,
                OverrideReason = d.OverrideReason,
                Notes = d.Notes
            });
        }

        public async Task<(Stream Stream, string ContentType)> GetAudioStreamWithAccessLogAsync(
            string storageKey,
            int userId,
            string userRole,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                throw new BusinessRuleException("Storage key không được để trống.");

            var answer = await _context.InterviewAnswers
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a => a.AudioStorageKey == storageKey, cancellationToken);

            if (answer == null)
            {
                var hadAccessLog = await _context.AudioAccessLogs.AnyAsync(l => l.StorageKey == storageKey, cancellationToken);
                if (hadAccessLog)
                {
                    throw new BusinessRuleException("File ghi âm này đã bị xóa vĩnh viễn theo chính sách quyền riêng tư dữ liệu cá nhân.", HttpStatusCode.Gone);
                }
                throw new NotFoundException("Audio file", storageKey);
            }

            // IDOR Protection: Interviewer chỉ được nghe audio thuộc buổi phỏng vấn được phân công
            if (userRole == nameof(UserRole.Interviewer))
            {
                if (answer.Interview.InterviewerId != userId)
                {
                    throw new BusinessRuleException("Bạn không được phân công phụ trách buổi phỏng vấn này nên không thể truy cập bản ghi âm.", HttpStatusCode.Forbidden);
                }
            }
            else if (userRole is not (nameof(UserRole.Admin) or nameof(UserRole.HR)))
            {
                throw new BusinessRuleException("Bạn không có quyền truy cập file ghi âm này.", HttpStatusCode.Forbidden);
            }

            if (_audioStorageService == null)
            {
                throw new InvalidOperationException("AudioStorageService is not configured.");
            }

            var fileExists = await _audioStorageService.ExistsAsync(storageKey, cancellationToken);
            if (!fileExists)
            {
                throw new BusinessRuleException("File ghi âm này không tồn tại hoặc đã bị xóa theo chính sách quyền riêng tư dữ liệu cá nhân.", HttpStatusCode.Gone);
            }

            var stream = await _audioStorageService.GetAudioStreamAsync(storageKey, cancellationToken);

            // Ghi nhật ký truy cập audio (Audit log cá nhân theo Luật Bảo vệ dữ liệu cá nhân (Luật số 91/2025/QH15))
            var accessLog = AudioAccessLog.Create(
                interviewId: answer.InterviewId,
                interviewAnswerId: answer.Id,
                storageKey: storageKey,
                userId: userId,
                userRole: userRole,
                ipAddress: ipAddress);

            _context.AudioAccessLogs.Add(accessLog);
            await _context.SaveChangesAsync(cancellationToken);

            _eventLogger.LogEvent("AudioAccessLog",
                $"Audio streamed (StorageKey: {storageKey}, UserId: {userId}, Role: {userRole})",
                _eventLogger.OnEntityCreated);

            var mimeType = "audio/webm";
            if (storageKey.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || storageKey.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase))
                mimeType = "audio/mp4";
            else if (storageKey.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                mimeType = "audio/wav";
            else if (storageKey.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                mimeType = "audio/mpeg";
            else if (storageKey.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                mimeType = "audio/ogg";

            return (stream, mimeType);
        }

        public async Task<PagedResult<InterviewDashboardSummaryDto>> GetDashboardSummaryAsync(
            InterviewDashboardFilterParams filters,
            CancellationToken cancellationToken = default)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var role = user?.GetRole();
            var currentUserId = user?.GetUserId();

            IQueryable<Interview> query = _context.Interviews.AsNoTracking();

            // 1) Phân quyền RBAC nghiêm ngặt:
            // - Interviewer: chỉ xem buổi phỏng vấn được gán cho chính mình
            // - Admin / HR: xem tất cả hoặc lọc theo InterviewerId cụ thể
            // - Candidate / khác: không có quyền xem dashboard
            if (role == nameof(UserRole.Interviewer) && currentUserId.HasValue)
            {
                query = query.Where(i => i.InterviewerId == currentUserId.Value);
            }
            else if (role is not (nameof(UserRole.Admin) or nameof(UserRole.HR)))
            {
                return new PagedResult<InterviewDashboardSummaryDto>
                {
                    Items = new List<InterviewDashboardSummaryDto>(),
                    TotalCount = 0,
                    PageNumber = filters.PageNumber,
                    PageSize = filters.PageSize
                };
            }
            else
            {
                if (filters.InterviewerId.HasValue)
                {
                    query = query.Where(i => i.InterviewerId == filters.InterviewerId.Value);
                }
            }

            // 2) Bộ lọc nâng cao:
            if (filters.JobPostingId.HasValue)
            {
                query = query.Where(i => i.Application.JobPostingId == filters.JobPostingId.Value);
            }

            if (filters.Status.HasValue)
            {
                query = query.Where(i => i.Status == filters.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Recommendation) &&
                Enum.TryParse<InterviewRecommendation>(filters.Recommendation, true, out var recEnum))
            {
                query = query.Where(i => i.Recommendation == recEnum);
            }

            if (!string.IsNullOrWhiteSpace(filters.DecisionStatus))
            {
                if (filters.DecisionStatus.Equals("Decided", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.ScreeningDecisions.Any());
                }
                else if (filters.DecisionStatus.Equals("PendingDecision", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => !i.ScreeningDecisions.Any() && i.Status == InterviewStatus.Completed);
                }
            }

            // 3) Tìm kiếm từ khóa (FullName, Email, Job Title)
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                var search = filters.Search.Trim();
                if (_context.Database.IsNpgsql())
                {
                    query = query.Where(i =>
                        EF.Functions.ILike(i.Application.JobPosting.Title, $"%{search}%") ||
                        EF.Functions.ILike(i.Application.Candidate.FullName, $"%{search}%") ||
                        EF.Functions.ILike(i.Application.Candidate.Email, $"%{search}%"));
                }
                else
                {
                    query = query.Where(i =>
                        i.Application.JobPosting.Title.ToLower().Contains(search.ToLower()) ||
                        i.Application.Candidate.FullName.ToLower().Contains(search.ToLower()) ||
                        i.Application.Candidate.Email.ToLower().Contains(search.ToLower()));
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // 4) Sắp xếp đa dạng với Nulls-Last cho cột AiOverallScore và Recommendation
            var sortBy = (filters.SortBy ?? "scheduleddate").ToLowerInvariant();
            var desc = filters.IsDescending;

            IOrderedQueryable<Interview> orderedQuery = sortBy switch
            {
                "aioverallscore" or "score" => desc
                    ? query.OrderByDescending(i => i.AiOverallScore.HasValue).ThenByDescending(i => i.AiOverallScore).ThenByDescending(i => i.Id)
                    : query.OrderByDescending(i => i.AiOverallScore.HasValue).ThenBy(i => i.AiOverallScore).ThenByDescending(i => i.Id),

                "recommendation" => desc
                    ? query.OrderByDescending(i => i.Recommendation.HasValue).ThenByDescending(i => i.Recommendation).ThenByDescending(i => i.Id)
                    : query.OrderByDescending(i => i.Recommendation.HasValue).ThenBy(i => i.Recommendation).ThenByDescending(i => i.Id),

                "candidatename" => desc
                    ? query.OrderByDescending(i => i.Application.Candidate.FullName).ThenByDescending(i => i.Id)
                    : query.OrderBy(i => i.Application.Candidate.FullName).ThenByDescending(i => i.Id),

                "jobtitle" => desc
                    ? query.OrderByDescending(i => i.Application.JobPosting.Title).ThenByDescending(i => i.Id)
                    : query.OrderBy(i => i.Application.JobPosting.Title).ThenByDescending(i => i.Id),

                "status" => desc
                    ? query.OrderByDescending(i => i.Status).ThenByDescending(i => i.Id)
                    : query.OrderBy(i => i.Status).ThenByDescending(i => i.Id),

                _ => desc
                    ? query.OrderByDescending(i => i.ScheduledDate).ThenByDescending(i => i.Id)
                    : query.OrderBy(i => i.ScheduledDate).ThenByDescending(i => i.Id)
            };

            // 5) Chiếu sang DTO tinh gọn qua LINQ .Select() thuần túy
            // Tuyệt đối không nạp transcript hay jsonb nặng vào memory
            var items = await orderedQuery
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(i => new InterviewDashboardSummaryDto
                {
                    Id = i.Id,
                    ApplicationId = i.ApplicationId,
                    CandidateName = i.Application.Candidate.FullName,
                    CandidateEmail = i.Application.Candidate.Email,
                    JobTitle = i.Application.JobPosting.Title,
                    JobPostingId = i.Application.JobPostingId,
                    InterviewerId = i.InterviewerId,
                    InterviewerName = i.Interviewer != null ? (i.Interviewer.Employee != null ? i.Interviewer.Employee.FullName : i.Interviewer.Email) : null,
                    ScheduledDate = i.ScheduledDate,
                    Status = i.Status.ToString(),
                    ApplicationStatus = i.Application.Status.ToString(),
                    AiOverallScore = i.AiOverallScore,
                    Recommendation = i.Recommendation != null ? i.Recommendation.ToString() : null,
                    TotalQuestions = i.Questions.Count,
                    ScoredQuestions = i.Questions.Count(q => q.Answer != null && q.Answer.Evaluations.Any(e => e.IsActive && e.Score != null)),
                    HasDecision = i.ScreeningDecisions.Any(),
                    LatestDecisionType = i.ScreeningDecisions
                        .OrderByDescending(d => d.DecidedAt)
                        .Select(d => d.DecisionType.ToString())
                        .FirstOrDefault(),
                    LatestDecisionAt = i.ScreeningDecisions
                        .OrderByDescending(d => d.DecidedAt)
                        .Select(d => (DateTime?)d.DecidedAt)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<InterviewDashboardSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize
            };
        }

        public async Task<InterviewDashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var role = user?.GetRole();
            var currentUserId = user?.GetUserId();

            IQueryable<Interview> query = _context.Interviews.AsNoTracking();

            if (role == nameof(UserRole.Interviewer) && currentUserId.HasValue)
            {
                query = query.Where(i => i.InterviewerId == currentUserId.Value);
            }
            else if (role is not (nameof(UserRole.Admin) or nameof(UserRole.HR)))
            {
                return new InterviewDashboardStatsDto();
            }

            var total = await query.CountAsync(cancellationToken);
            var pendingOrInProgress = await query.CountAsync(i => i.Status == InterviewStatus.Scheduled || i.Status == InterviewStatus.InProgress, cancellationToken);
            var completed = await query.CountAsync(i => i.Status == InterviewStatus.Completed, cancellationToken);
            var pass = await query.CountAsync(i => i.Recommendation == InterviewRecommendation.Pass, cancellationToken);
            var consider = await query.CountAsync(i => i.Recommendation == InterviewRecommendation.Consider, cancellationToken);
            var reject = await query.CountAsync(i => i.Recommendation == InterviewRecommendation.Reject, cancellationToken);
            var decided = await query.CountAsync(i => i.ScreeningDecisions.Any(), cancellationToken);
            var pendingDecisions = await query.CountAsync(i => i.Status == InterviewStatus.Completed && !i.ScreeningDecisions.Any(), cancellationToken);

            return new InterviewDashboardStatsDto
            {
                TotalInterviews = total,
                PendingOrInProgress = pendingOrInProgress,
                Completed = completed,
                RecommendedPass = pass,
                RecommendedConsider = consider,
                RecommendedReject = reject,
                PendingDecisions = pendingDecisions,
                Decided = decided
            };
        }

        public async Task<List<InterviewerOptionDto>> GetAvailableInterviewersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Interviewer)
                .Include(u => u.Employee)
                .OrderBy(u => u.Employee != null ? u.Employee.FullName : u.Email)
                .Select(u => new InterviewerOptionDto
                {
                    Id = u.Id,
                    Name = u.Employee != null ? u.Employee.FullName : u.Email,
                    Email = u.Email
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<ErasePersonalDataResponseDto> ErasePersonalDataAsync(
            int interviewId,
            ErasePersonalDataDto dto,
            int currentUserId,
            string currentRole,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                throw new BusinessRuleException("Lý do xóa dữ liệu cá nhân không được để trống.");
            }

            var interview = await _context.Interviews
                .Include(i => i.Answers)
                    .ThenInclude(a => a.Evaluations)
                .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken)
                ?? throw new NotFoundException("Interview", interviewId);

            var erasedAnswersCount = 0;
            var erasedAudioFilesCount = 0;
            var erasedEvaluationsCount = 0;

            foreach (var answer in interview.Answers)
            {
                if (!string.IsNullOrWhiteSpace(answer.AudioStorageKey))
                {
                    if (_audioStorageService != null)
                    {
                        try
                        {
                            await _audioStorageService.DeleteAudioAsync(answer.AudioStorageKey, cancellationToken);
                            erasedAudioFilesCount++;
                        }
                        catch
                        {
                            // File vật lý có thể đã bị xóa trước đó hoặc không khả dụng, tiếp tục làm sạch DB
                        }
                    }
                }

                answer.AnonymizePersonalData();
                erasedAnswersCount++;

                foreach (var eval in answer.Evaluations)
                {
                    eval.AnonymizePersonalData();
                    erasedEvaluationsCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log: ghi nhận ai xóa, khi nào, lý do, interviewId (TUYỆT ĐỐI không chứa nội dung cá nhân)
            _eventLogger.LogEvent("InterviewPersonalData",
                $"erased for Interview #{interviewId} by User #{currentUserId} ({currentRole}). Reason: {dto.Reason.Trim()}",
                _eventLogger.OnEntityDeleted);

            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
            var userName = user?.Employee?.FullName ?? user?.Email ?? $"User #{currentUserId}";

            return new ErasePersonalDataResponseDto
            {
                InterviewId = interview.Id,
                Message = "Toàn bộ dữ liệu cá nhân (file ghi âm, transcript, trích dẫn bằng chứng và phân tích) đã được xóa sạch theo chính sách quyền riêng tư.",
                ErasedAnswersCount = erasedAnswersCount,
                ErasedAudioFilesCount = erasedAudioFilesCount,
                ErasedEvaluationsCount = erasedEvaluationsCount,
                ErasedAt = DateTime.UtcNow,
                ErasedByUserName = userName,
                Reason = dto.Reason.Trim()
            };
        }
    }
}
