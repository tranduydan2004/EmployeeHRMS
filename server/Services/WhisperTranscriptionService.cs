using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Audio;

namespace EmployeeHRMS.Api.Services
{
    /// <summary>
    /// Triển khai ISpeechToTextService sử dụng OpenAI Whisper API (model whisper-1) qua Official .NET SDK.
    /// Cấu hình verbose_json + word-level timestamps để hỗ trợ Phase 4 phân tích tốc độ nói.
    /// Có cơ chế retry thủ công với exponential backoff cho transient errors (429, 5xx, timeout).
    /// </summary>
    public class WhisperTranscriptionService : ISpeechToTextService
    {
        private readonly AudioClient _audioClient;
        private readonly ILogger<WhisperTranscriptionService> _logger;

        // Retry configuration
        private const int MaxRetries = 3;
        private static readonly TimeSpan InitialBackoff = TimeSpan.FromSeconds(2);
        private const double BackoffMultiplier = 2.0;
        private const double JitterFactor = 0.3;

        public WhisperTranscriptionService(
            IOptions<OpenAiSettings> settings,
            ILogger<WhisperTranscriptionService> logger)
        {
            _logger = logger;

            var config = settings.Value;
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new InvalidOperationException("OpenAiSettings:ApiKey is not configured for Whisper STT service.");
            }

            var model = string.IsNullOrWhiteSpace(config.WhisperModel) ? "whisper-1" : config.WhisperModel;
            _audioClient = new AudioClient(model, config.ApiKey);
        }

        public WhisperTranscriptionService(
            AudioClient audioClient,
            ILogger<WhisperTranscriptionService> logger)
        {
            _audioClient = audioClient ?? throw new ArgumentNullException(nameof(audioClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TranscriptionResult> TranscribeAsync(
            Stream audioStream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            if (audioStream == null)
            {
                throw new ArgumentNullException(nameof(audioStream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required for Whisper API format detection.", nameof(fileName));
            }

            // Rewind stream nếu cần
            if (audioStream.CanSeek)
            {
                audioStream.Position = 0;
            }

            var options = new AudioTranscriptionOptions
            {
                ResponseFormat = AudioTranscriptionFormat.Verbose,
                TimestampGranularities = AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment
            };

            // Retry loop với exponential backoff cho transient errors
            Exception? lastException = null;
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        // Exponential backoff với jitter
                        var delay = CalculateBackoffDelay(attempt);
                        _logger.LogWarning(
                            "Whisper API retry attempt {Attempt}/{MaxRetries} after {Delay}ms for file {FileName}",
                            attempt, MaxRetries, delay.TotalMilliseconds, fileName);

                        await Task.Delay(delay, cancellationToken);

                        // Rewind stream cho retry
                        if (audioStream.CanSeek)
                        {
                            audioStream.Position = 0;
                        }
                        else
                        {
                            _logger.LogError("Cannot retry Whisper API call: audio stream is not seekable for file {FileName}", fileName);
                            throw lastException ?? new InvalidOperationException("Audio stream is not seekable for retry.");
                        }
                    }

                    _logger.LogInformation(
                        "Calling Whisper API for file {FileName} (attempt {Attempt})",
                        fileName, attempt + 1);

                    var transcription = await _audioClient.TranscribeAudioAsync(
                        audioStream,
                        fileName,
                        options,
                        cancellationToken);

                    var result = MapToResult(transcription.Value);

                    _logger.LogInformation(
                        "Whisper API transcription completed for {FileName}: {TextLength} chars, language={Language}, duration={Duration}s",
                        fileName, result.Text.Length, result.DetectedLanguage, result.DurationSeconds);

                    return result;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Graceful shutdown — không retry
                    throw;
                }
                catch (Exception ex) when (IsTransientError(ex))
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "Transient error calling Whisper API for {FileName} (attempt {Attempt}/{MaxRetries}): {Error}",
                        fileName, attempt + 1, MaxRetries + 1, ex.Message);

                    if (attempt == MaxRetries)
                    {
                        _logger.LogError(ex,
                            "Whisper API failed after {MaxRetries} retries for file {FileName}",
                            MaxRetries + 1, fileName);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // Non-transient error (400 Bad Request, 401 Unauthorized, v.v.) — fail fast
                    _logger.LogError(ex,
                        "Non-transient error calling Whisper API for {FileName}: {Error}",
                        fileName, ex.Message);
                    throw;
                }
            }

            // Không bao giờ tới đây nhưng compiler cần
            throw lastException ?? new InvalidOperationException("Unexpected state in retry loop.");
        }

        /// <summary>
        /// Map AudioTranscription response từ OpenAI SDK sang domain DTO TranscriptionResult.
        /// Serialize words/segments thành JSON string cho lưu trữ cột jsonb trong DB.
        /// </summary>
        private static TranscriptionResult MapToResult(AudioTranscription transcription)
        {
            string? segmentsJson = null;

            // Serialize cả words và segments thành JSON compact cho cột TranscriptSegments (jsonb)
            var verboseData = new
            {
                words = SerializeWords(transcription),
                segments = SerializeSegments(transcription)
            };

            segmentsJson = JsonSerializer.Serialize(verboseData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            return new TranscriptionResult
            {
                Text = transcription.Text ?? string.Empty,
                SegmentsJson = segmentsJson,
                DetectedLanguage = transcription.Language,
                DurationSeconds = transcription.Duration?.TotalSeconds
            };
        }

        /// <summary>
        /// Serialize word-level timestamps từ AudioTranscription.
        /// </summary>
        private static object[] SerializeWords(AudioTranscription transcription)
        {
            if (transcription.Words == null || transcription.Words.Count == 0)
                return Array.Empty<object>();

            var words = new object[transcription.Words.Count];
            for (int i = 0; i < transcription.Words.Count; i++)
            {
                var w = transcription.Words[i];
                words[i] = new
                {
                    word = w.Word,
                    start = w.StartTime.TotalSeconds,
                    end = w.EndTime.TotalSeconds
                };
            }
            return words;
        }

        /// <summary>
        /// Serialize segment-level data từ AudioTranscription.
        /// </summary>
        private static object[] SerializeSegments(AudioTranscription transcription)
        {
            if (transcription.Segments == null || transcription.Segments.Count == 0)
                return Array.Empty<object>();

            var segments = new object[transcription.Segments.Count];
            for (int i = 0; i < transcription.Segments.Count; i++)
            {
                var s = transcription.Segments[i];
                segments[i] = new
                {
                    id = s.Id,
                    start = s.StartTime.TotalSeconds,
                    end = s.EndTime.TotalSeconds,
                    text = s.Text,
                    noSpeechProbability = s.NoSpeechProbability
                };
            }
            return segments;
        }

        /// <summary>
        /// Phân biệt transient errors (nên retry) vs non-transient errors (fail fast).
        /// Transient: HTTP 429 (Rate Limit), 5xx (Server errors), Timeout, Network errors.
        /// Non-transient: HTTP 400 (Bad Request — file hỏng), 401 (API key sai), 403.
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            // HttpRequestException chứa status code
            if (ex is HttpRequestException httpEx)
            {
                var statusCode = (int?)httpEx.StatusCode;
                // 429 Rate Limit, 500+ Server errors
                return statusCode is 429 or >= 500;
            }

            // TaskCanceledException từ HttpClient timeout (không phải cancellation thật)
            if (ex is TaskCanceledException && ex.InnerException is TimeoutException)
            {
                return true;
            }

            // IOException — network connection issues
            if (ex is IOException)
            {
                return true;
            }

            // ClientResultException từ OpenAI SDK — parse status code từ message
            var exTypeName = ex.GetType().Name;
            if (exTypeName == "ClientResultException" && ex.Message != null)
            {
                // OpenAI SDK throws ClientResultException with status code in message
                if (ex.Message.Contains("429") || ex.Message.Contains("500") ||
                    ex.Message.Contains("502") || ex.Message.Contains("503") ||
                    ex.Message.Contains("504"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tính delay exponential backoff có jitter ngẫu nhiên.
        /// Jitter tránh thundering herd khi nhiều worker retry cùng lúc.
        /// </summary>
        private static TimeSpan CalculateBackoffDelay(int attempt)
        {
            var baseDelay = InitialBackoff.TotalMilliseconds * Math.Pow(BackoffMultiplier, attempt - 1);
            var jitter = baseDelay * JitterFactor * (Random.Shared.NextDouble() * 2 - 1); // ±30%
            var totalMs = Math.Max(100, baseDelay + jitter); // tối thiểu 100ms
            return TimeSpan.FromMilliseconds(Math.Min(totalMs, 30_000)); // tối đa 30s
        }
    }
}
