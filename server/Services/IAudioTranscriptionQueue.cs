using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Interface trừu tượng hóa việc đẩy job vào hàng đợi bóc băng.
    /// Controller/Service gọi EnqueueAsync — consumer (TranscriptionWorker) đọc từ phía Reader.
    /// </summary>
    public interface IAudioTranscriptionQueue
    {
        /// <summary>
        /// Đẩy một job bóc băng vào hàng đợi in-memory (bounded channel).
        /// </summary>
        ValueTask EnqueueAsync(AudioTranscriptionJob job, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reader phía consumer — TranscriptionWorker dùng ReadAllAsync() để đọc job.
        /// </summary>
        ChannelReader<AudioTranscriptionJob> Reader { get; }
    }
}
