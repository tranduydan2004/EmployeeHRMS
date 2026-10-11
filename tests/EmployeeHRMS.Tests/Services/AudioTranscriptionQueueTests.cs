using System;
using System.Threading;
using System.Threading.Tasks;
using EmployeeHRMS.Api.Models;
using EmployeeHRMS.Api.Services;
using FluentAssertions;
using Xunit;

namespace EmployeeHRMS.Tests.Services
{
    public class AudioTranscriptionQueueTests
    {
        private readonly AudioTranscriptionQueue _queue;

        public AudioTranscriptionQueueTests()
        {
            _queue = new AudioTranscriptionQueue();
        }

        [Fact]
        public async Task EnqueueAsync_ShouldEnqueueJobAndBeReadableByConsumer()
        {
            // Arrange
            var job = new AudioTranscriptionJob
            {
                InterviewAnswerId = 42,
                EnqueuedAt = DateTime.UtcNow
            };

            // Act
            await _queue.EnqueueAsync(job);

            // Assert
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var canRead = await _queue.Reader.WaitToReadAsync(cts.Token);
            canRead.Should().BeTrue();

            var readSuccess = _queue.Reader.TryRead(out var dequeuedJob);
            readSuccess.Should().BeTrue();
            dequeuedJob.Should().NotBeNull();
            dequeuedJob!.InterviewAnswerId.Should().Be(42);
        }

        [Fact]
        public async Task EnqueueAsync_MultipleJobs_ShouldPreserveFIFOOrder()
        {
            // Arrange
            var job1 = new AudioTranscriptionJob { InterviewAnswerId = 101 };
            var job2 = new AudioTranscriptionJob { InterviewAnswerId = 102 };
            var job3 = new AudioTranscriptionJob { InterviewAnswerId = 103 };

            // Act
            await _queue.EnqueueAsync(job1);
            await _queue.EnqueueAsync(job2);
            await _queue.EnqueueAsync(job3);

            // Assert
            _queue.Reader.TryRead(out var dequeued1).Should().BeTrue();
            dequeued1!.InterviewAnswerId.Should().Be(101);

            _queue.Reader.TryRead(out var dequeued2).Should().BeTrue();
            dequeued2!.InterviewAnswerId.Should().Be(102);

            _queue.Reader.TryRead(out var dequeued3).Should().BeTrue();
            dequeued3!.InterviewAnswerId.Should().Be(103);
        }
    }
}
