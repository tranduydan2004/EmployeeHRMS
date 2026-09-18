namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Local File Storage Service — lưu file vào wwwroot/uploads/resumes/.
    /// GUID filename chống trùng tên + Path Traversal.
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        public LocalFileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveResumeAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
                "uploads", "resumes");

            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            // GUID filename — chống trùng tên + Path Traversal
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Trả về relative path (không chứa wwwroot prefix)
            return $"uploads/resumes/{fileName}";
        }

        public Task DeleteFileAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;

            var fullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
                relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }
    }
}
