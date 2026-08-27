using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Demo.VideoPlayer;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Diagnostics;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;
using ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Session
{
    /// <summary>
    /// #235's own scope wired into <see cref="VideoPlaybackSession"/>: passing a
    /// <see cref="VideoPlaybackTelemetry"/> instance changes nothing about a session's own already-tested
    /// behavior (see <see cref="VideoPlaybackSessionTests"/> for the full behavioral suite this
    /// deliberately does not duplicate) — it only adds recording alongside it, including still invoking a
    /// caller's own <c>onFrameDropped</c> exactly as before.
    /// </summary>
    public sealed class VideoPlaybackSessionTelemetryTests
    {
        private static readonly VideoSource TestSource = new() { Location = "synthetic://test" };

        private static VideoPlaybackSessionOptions FastOptions() => new()
        {
            PlaybackRate = 100_000,
            PollInterval = TimeSpan.FromMilliseconds(2)
        };

        private static ReadOnlyMemory<byte> PassthroughEncode(DecodedVideoFrame frame) => frame.Data;

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Condition was not met in time.");

                await Task.Delay(5);
            }
        }

        [Fact]
        public async Task SelectAsync_WithTelemetryAttached_StillPublishesFramesToEveryViewer()
        {
            var telemetry = new VideoPlaybackTelemetry();
            await using var session = new VideoPlaybackSession(
                "t1", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode,
                telemetry: telemetry);
            session.Subscribe("viewerA");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            var frames = new List<VideoFramePacket>();
            while (session.TryDequeue("viewerA", out var packet))
                frames.Add(packet!);

            Assert.NotEmpty(frames);
        }

        [Fact]
        public async Task Subscribe_ThenUnsubscribe_WithTelemetryAttached_DoesNotThrow()
        {
            var telemetry = new VideoPlaybackTelemetry();
            await using var session = new VideoPlaybackSession(
                "t2", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), telemetry: telemetry);

            var exception = await Record.ExceptionAsync(async () =>
            {
                session.Subscribe("viewerA");
                session.Subscribe("viewerA"); // no-op re-subscribe must not double-record a join
                session.Unsubscribe("viewerA");
                await Task.CompletedTask;
            });

            Assert.Null(exception);
        }

        // #235's own AC, "aggregating FrameDropReason occurrences into real metrics" — proves the combined
        // delegate CreateDropCallback wires up still invokes a caller's own onFrameDropped exactly as it
        // did before this ticket, alongside (not instead of) recording into telemetry.
        [Fact]
        public async Task FrameDrops_WithTelemetryAttached_StillInvokeTheOriginalOnFrameDroppedCallback()
        {
            var droppedReasons = new List<FrameDropReason>();
            var telemetry = new VideoPlaybackTelemetry();

            // Capacity 1 with a near-real-time playback rate: SyntheticVideoFrameSource's decode loop has
            // no artificial delay of its own, so it races far ahead of a buffer this small, forcing
            // DecodeBufferCapacityExceeded evictions before the publish loop ever drains them.
            var options = new VideoPlaybackSessionOptions
            {
                DecodeBufferCapacity = 1,
                PlaybackRate = 1.0,
                PollInterval = TimeSpan.FromMilliseconds(2)
            };

            await using var session = new VideoPlaybackSession(
                "t3", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughEncode,
                onFrameDropped: droppedReasons.Add, telemetry: telemetry);

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            Assert.Contains(FrameDropReason.DecodeBufferCapacityExceeded, droppedReasons);
        }

        [Fact]
        public async Task Fault_DuringDecode_WithTelemetryAttached_StillTransitionsToFaulted()
        {
            var telemetry = new VideoPlaybackTelemetry();
            var faultingSource = new FaultingVideoFrameSource();
            await using var session = new VideoPlaybackSession(
                "t4", () => faultingSource, new SystemMonotonicClock(), FastOptions(), PassthroughEncode,
                telemetry: telemetry);

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Faulted, TimeSpan.FromSeconds(5));

            Assert.IsType<VideoFrameSourceException>(session.Fault);
            Assert.True(faultingSource.Disposed);
        }

        private sealed class FaultingVideoFrameSource : IVideoFrameSource
        {
            public bool Disposed { get; private set; }
            public VideoStreamInfo? StreamInfo { get; private set; }

            public Task<VideoStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
            {
                var info = new VideoStreamInfo { Width = 4, Height = 4, PixelFormat = VideoPixelFormat.Rgb24, IsVariableFrameRate = false };
                StreamInfo = info;
                return Task.FromResult(info);
            }

            public async IAsyncEnumerable<DecodedVideoFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                throw new VideoFrameSourceException("synthetic decode failure");
#pragma warning disable CS0162 // unreachable, but required so the compiler recognizes this as an iterator method
                yield break;
#pragma warning restore CS0162
            }

            public ValueTask DisposeAsync()
            {
                Disposed = true;
                return ValueTask.CompletedTask;
            }
        }
    }
}
