using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Triển khai Channel-based in-memory queue cho audio transcription jobs.
    /// Bounded capacity = 100 (back-pressure: nếu queue đầy, producer sẽ chờ không block thread).
    /// Đăng ký Singleton vì Channel chia sẻ giữa nhiều Scoped request và 1 Singleton BackgroundService.
    /// </summary>
    public class AudioTranscriptionQueue : IAudioTranscriptionQueue
    {
        private readonly Channel<AudioTranscriptionJob> _channel;

        public AudioTranscriptionQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,    // Chỉ có 1 TranscriptionWorker đọc
                SingleWriter = false    // Nhiều request có thể ghi đồng thời
            };

            _channel = Channel.CreateBounded<AudioTranscriptionJob>(options);
        }

        public ValueTask EnqueueAsync(AudioTranscriptionJob job, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(job, cancellationToken);
        }

        public ChannelReader<AudioTranscriptionJob> Reader => _channel.Reader;
    }
}
