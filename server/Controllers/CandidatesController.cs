using EmployeeHRMS.Api.Authorization;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.DTOs.Common;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeHRMS.Api.Controllers
{
    /// <summary>
    /// Candidates Controller — CRUD + search endpoint
    /// Resource-Based Authorization: GetById/Update/Delete dùng IAuthorizationService.
    /// GetAll lọc tự động theo Role trong Service layer.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Route: api/candidates
    [Authorize]
    public class CandidatesController : ControllerBase
    {
        private readonly ICandidateService _candidateService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileValidatorService _fileValidator;
        private readonly IWebHostEnvironment _env;

        public CandidatesController(
            ICandidateService candidateService,
            IAuthorizationService authorizationService,
            IFileStorageService fileStorageService,
            IFileValidatorService fileValidator,
            IWebHostEnvironment env)
        {
            _candidateService = candidateService;
            _authorizationService = authorizationService;
            _fileStorageService = fileStorageService;
            _fileValidator = fileValidator;
            _env = env;
        }

        // GET: api/candidates — Lọc tự động theo Role trong Service
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
        {
            var result = await _candidateService.GetAllAsync(paginationParams);
            return Ok(result);
        }

        // GET: api/candidates/{id} — Resource-Based Authorization
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var candidate = await _candidateService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Candidate", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, candidate, ResourceOperations.Read);
            if (!authResult.Succeeded) return Forbid();

            var dto = await _candidateService.GetByIdAsync(id);
            return Ok(dto);
        }

        // GET: api/candidates/search?keyword={keyword} [DEPRECATED]
        [HttpGet("search")]
        [Obsolete("Use GET /api/candidates with paginationParams.Search instead.")]
        public async Task<IActionResult> Search([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { error = "Search keyword cannot be empty." });

            var candidates = await _candidateService.SearchAsync(keyword);
            return Ok(candidates);
        }

        // POST: api/candidates
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CandidateCreateDto dto)
        {
            var candidate = await _candidateService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = candidate.Id }, candidate);
        }

        // PUT: api/candidates/{id} — Resource-Based Authorization
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CandidateUpdateDto dto)
        {
            var candidate = await _candidateService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Candidate", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, candidate, ResourceOperations.Update);
            if (!authResult.Succeeded) return Forbid();

            await _candidateService.UpdateAsync(id, dto);
            return NoContent();
        }

        // DELETE: api/candidates/{id} — Resource-Based Authorization
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var candidate = await _candidateService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Candidate", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, candidate, ResourceOperations.Delete);
            if (!authResult.Succeeded) return Forbid();

            await _candidateService.DeleteAsync(id);
            return NoContent();
        }

        // ============================================================
        // Resume Upload / Download — Resource-Based Authorization
        // ============================================================

        /// <summary>
        /// POST: api/candidates/{id}/resume (multipart/form-data)
        /// Upload CV/Resume — 4 lớp bảo mật chuyên sâu (Magic Bytes, OOXML, MIME, Extension, Size).
        /// AuthorizeAsync(User, candidate, ResourceOperations.Update) — Candidate chỉ upload cho chính mình.
        /// </summary>
        [HttpPost("{id}/resume")]
        public async Task<IActionResult> UploadResume(int id, IFormFile file)
        {
            // Resource-based auth
            var candidate = await _candidateService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Candidate", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, candidate, ResourceOperations.Update);
            if (!authResult.Succeeded) return Forbid();

            // 1. Xác thực file 4 lớp (FAIL sẽ throw BusinessRuleException, dừng ngay mà không chạm tới đĩa)
            await _fileValidator.ValidateResumeFileAsync(file);

            // 2. CHỈ KHI validate pass hoàn toàn: xoá file cũ nếu đã có
            if (!string.IsNullOrWhiteSpace(candidate.ResumeUrl))
                await _fileStorageService.DeleteFileAsync(candidate.ResumeUrl);

            // 3. Sinh GUID tên file, ghi file mới xuống disk
            var relativePath = await _fileStorageService.SaveResumeAsync(file);

            // 4. Cập nhật ResumeUrl
            candidate.ResumeUrl = relativePath;
            await _candidateService.UpdateAsync(id, new CandidateUpdateDto { ResumeUrl = relativePath });

            return Ok(new { resumeUrl = relativePath });
        }

        /// <summary>
        /// GET: api/candidates/{id}/resume
        /// Download CV/Resume — AuthorizeAsync(User, candidate, ResourceOperations.Read).
        /// Trả file qua FileStreamResult, KHÔNG dùng static files.
        /// </summary>
        [HttpGet("{id}/resume")]
        public async Task<IActionResult> DownloadResume(int id)
        {
            var candidate = await _candidateService.GetEntityByIdAsync(id)
                ?? throw new NotFoundException("Candidate", id);

            var authResult = await _authorizationService.AuthorizeAsync(User, candidate, ResourceOperations.Read);
            if (!authResult.Succeeded) return Forbid();

            if (string.IsNullOrWhiteSpace(candidate.ResumeUrl))
                return NotFound(new { error = "Candidate does not have a resume uploaded." });

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot, candidate.ResumeUrl.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { error = "Resume file not found on server." });

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(stream, contentType, Path.GetFileName(filePath));
        }
    }
}

