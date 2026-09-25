using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeHRMS.Api.Controllers
{
    /// <summary>
    /// JobPostings Controller — CRUD + filter endpoints
    /// Quyền truy cập: Admin, HR (CRUD). GET endpoints public (AllowAnonymous) — trang tuyển dụng.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Route: api/jobpostings
    [Authorize(Roles = "Admin,HR")]
    public class JobPostingsController : ControllerBase
    {
        private readonly IJobPostingService _jobPostingService;

        public JobPostingsController(IJobPostingService jobPostingService)
        {
            _jobPostingService = jobPostingService;
        }

        // GET: api/jobpostings — Public (trang tuyển dụng) / Admin (quản trị)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] JobPostingQueryParams queryParams)
        {
            var isAdminOrHr = User.IsInRole("Admin") || User.IsInRole("HR");
            if (!isAdminOrHr)
            {
                // ÉP CỨNG status = Published đối với Anonymous, Candidate, Interviewer
                queryParams.Status = Models.JobPostingStatus.Published;
            }

            var result = await _jobPostingService.GetAllAsync(queryParams);
            return Ok(result);
        }

        // GET: api/jobpostings/{id} — Public
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var job = await _jobPostingService.GetByIdAsync(id);
            return Ok(job);
        }

        // GET: api/jobpostings/active — Public
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveJobs()
        {
            var jobs = await _jobPostingService.GetActiveJobsAsync();
            return Ok(jobs);
        }

        // GET: api/jobpostings/by-department/{departmentId} — Public
        [HttpGet("by-department/{departmentId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByDepartment(int departmentId)
        {
            var jobs = await _jobPostingService.GetByDepartmentAsync(departmentId);
            return Ok(jobs);
        }

        // POST: api/jobpostings — Admin, HR only
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JobPostingCreateDto dto)
        {
            var job = await _jobPostingService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
        }

        // PUT: api/jobpostings/{id} — Admin, HR only
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] JobPostingUpdateDto dto)
        {
            await _jobPostingService.UpdateAsync(id, dto);
            return NoContent();
        }

        // DELETE: api/jobpostings/{id} — Admin, HR only
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _jobPostingService.DeleteAsync(id);
            return NoContent();
        }

        // ================================================================
        // Phase 1: Smart JD & Question Bank Generation Endpoints
        // ================================================================

        // POST: api/jobpostings/draft-jd — Admin, HR only
        [HttpPost("draft-jd")]
        public async Task<IActionResult> DraftJd([FromBody] DraftJdRequestDto dto)
        {
            var createdBy = User.Identity?.Name ?? "HR";
            var result = await _jobPostingService.DraftJdAsync(dto, createdBy);
            return CreatedAtAction(nameof(GetDetailById), new { id = result.Id }, result);
        }

        // PUT: api/jobpostings/{id}/jd-content — Admin, HR only
        [HttpPut("{id}/jd-content")]
        public async Task<IActionResult> UpdateJdContent(int id, [FromBody] UpdateJdContentDto dto)
        {
            var result = await _jobPostingService.UpdateJdContentAsync(id, dto);
            return Ok(result);
        }

        // POST: api/jobpostings/{id}/approve — Admin, HR only
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveJd(int id)
        {
            var result = await _jobPostingService.ApproveJdAsync(id);
            return Ok(result);
        }

        // GET: api/jobpostings/{id}/detail — Admin, HR only
        [HttpGet("{id}/detail")]
        public async Task<IActionResult> GetDetailById(int id)
        {
            var result = await _jobPostingService.GetDetailByIdAsync(id);
            return Ok(result);
        }

        // GET: api/jobpostings/{id}/questions — Admin, HR only
        [HttpGet("{id}/questions")]
        public async Task<IActionResult> GetQuestions(int id)
        {
            var result = await _jobPostingService.GetQuestionsAsync(id);
            return Ok(result);
        }
    }
}
