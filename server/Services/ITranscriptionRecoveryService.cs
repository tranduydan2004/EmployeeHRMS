using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.DTOs;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Service quét và phục hồi các tác vụ bóc băng bị kẹt (Zombie Jobs) do server crash/restart
    /// hoặc bị timeout trong quá trình xử lý ngầm.
    /// </summary>
    public interface ITranscriptionRecoveryService
    {
        /// <summary>
        /// Quét và phục hồi các tác vụ bóc băng bị gián đoạn.
        /// </summary>
        /// <param name="isStartup">
        /// true nếu gọi khi server khởi động (coi TẤT CẢ job Transcribing là zombie và requeue tất cả job Pending);
        /// false nếu gọi định kỳ runtime (chỉ coi các job Transcribing có ProcessingStartedAt > timeout là zombie).
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Kết quả thống kê số lượng job đã được phục hồi, thất bại hoặc đưa lại vào queue</returns>
        Task<TranscriptionRecoveryResponseDto> RecoverZombieJobsAsync(
            bool isStartup = true,
            CancellationToken cancellationToken = default);
    }
}
