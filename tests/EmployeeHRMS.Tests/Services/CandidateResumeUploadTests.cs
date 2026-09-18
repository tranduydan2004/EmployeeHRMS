using EmployeeHRMS.Api.Authorization;
using EmployeeHRMS.Api.Controllers;
using EmployeeHRMS.Api.DTOs;
using EmployeeHRMS.Api.Exceptions;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace EmployeeHRMS.Tests.Services
{
    public class CandidateResumeUploadTests
    {
        private readonly Mock<ICandidateService> _mockCandidateService;
        private readonly Mock<IAuthorizationService> _mockAuthService;
        private readonly Mock<IFileStorageService> _mockFileStorage;
        private readonly Mock<IFileValidatorService> _mockFileValidator;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly CandidatesController _controller;

        public CandidateResumeUploadTests()
        {
            _mockCandidateService = new Mock<ICandidateService>();
            _mockAuthService = new Mock<IAuthorizationService>();
            _mockFileStorage = new Mock<IFileStorageService>();
            _mockFileValidator = new Mock<IFileValidatorService>();
            _mockEnv = new Mock<IWebHostEnvironment>();

            _controller = new CandidatesController(
                _mockCandidateService.Object,
                _mockAuthService.Object,
                _mockFileStorage.Object,
                _mockFileValidator.Object,
                _mockEnv.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Candidate")
            }, "TestAuth"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task UploadResume_WhenValidationFails_DoesNotDeleteExistingResumeFile()
        {
            // Arrange — Candidate đã có CV cũ trên hệ thống
            const int candidateId = 1;
            const string existingResumeUrl = "uploads/resumes/existing-guid.pdf";

            var candidate = new Candidate
            {
                Id = candidateId,
                FullName = "Nguyễn Văn A",
                Email = "a@test.com",
                ResumeUrl = existingResumeUrl,
                UserId = 1
            };

            _mockCandidateService.Setup(s => s.GetEntityByIdAsync(candidateId))
                .ReturnsAsync(candidate);

            _mockAuthService.Setup(s => s.AuthorizeAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    candidate,
                    It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                .ReturnsAsync(AuthorizationResult.Success());

            // Mock file mới (giả mạo, validator sẽ ném BusinessRuleException)
            var mockNewFile = new Mock<IFormFile>();
            _mockFileValidator.Setup(v => v.ValidateResumeFileAsync(mockNewFile.Object))
                .ThrowsAsync(new BusinessRuleException("Nội dung file không đúng với định dạng khai báo (Giả mạo định dạng)."));

            // Act
            var act = () => _controller.UploadResume(candidateId, mockNewFile.Object);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            // File cũ TUYỆT ĐỐI KHÔNG được gọi xóa
            _mockFileStorage.Verify(f => f.DeleteFileAsync(It.IsAny<string>()), Times.Never);

            // File mới không được ghi
            _mockFileStorage.Verify(f => f.SaveResumeAsync(It.IsAny<IFormFile>()), Times.Never);

            // Candidate.ResumeUrl không bị cập nhật
            _mockCandidateService.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<CandidateUpdateDto>()), Times.Never);
            candidate.ResumeUrl.Should().Be(existingResumeUrl);
        }

        [Fact]
        public async Task UploadResume_WhenValidationSucceeds_DeletesOldFileAndSavesNewFile()
        {
            // Arrange
            const int candidateId = 1;
            const string existingResumeUrl = "uploads/resumes/old-resume.pdf";
            const string newResumeUrl = "uploads/resumes/new-resume.pdf";

            var candidate = new Candidate
            {
                Id = candidateId,
                FullName = "Nguyễn Văn A",
                Email = "a@test.com",
                ResumeUrl = existingResumeUrl,
                UserId = 1
            };

            _mockCandidateService.Setup(s => s.GetEntityByIdAsync(candidateId))
                .ReturnsAsync(candidate);

            _mockAuthService.Setup(s => s.AuthorizeAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    candidate,
                    It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                .ReturnsAsync(AuthorizationResult.Success());

            var mockNewFile = new Mock<IFormFile>();
            _mockFileValidator.Setup(v => v.ValidateResumeFileAsync(mockNewFile.Object))
                .Returns(Task.CompletedTask);

            _mockFileStorage.Setup(f => f.SaveResumeAsync(mockNewFile.Object))
                .ReturnsAsync(newResumeUrl);

            // Act
            var result = await _controller.UploadResume(candidateId, mockNewFile.Object);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // File cũ bị xóa SAU KHI validate pass
            _mockFileStorage.Verify(f => f.DeleteFileAsync(existingResumeUrl), Times.Once);

            // File mới được lưu
            _mockFileStorage.Verify(f => f.SaveResumeAsync(mockNewFile.Object), Times.Once);

            // Candidate.ResumeUrl được cập nhật sang file mới
            _mockCandidateService.Verify(s => s.UpdateAsync(candidateId, It.Is<CandidateUpdateDto>(d => d.ResumeUrl == newResumeUrl)), Times.Once);
        }
    }
}
