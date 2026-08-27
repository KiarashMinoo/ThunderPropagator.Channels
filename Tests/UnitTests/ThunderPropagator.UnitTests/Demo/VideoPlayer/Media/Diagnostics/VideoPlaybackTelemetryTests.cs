using System.Diagnostics;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Diagnostics;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Diagnostics
{
    /// <summary>
    /// #235's own ACs for <see cref="VideoPlaybackTelemetry"/> in isolation: every Record* method is
    /// callable without a listener attached (this test host never calls
    /// ThunderPropagator.BuildingBlocks.Application.Telemetry.Configure, so every instrument this type
    /// creates is null — see that type's own remarks), and its own hot-path overhead stays bounded.
    /// </summary>
    public sealed class VideoPlaybackTelemetryTests
    {
        [Fact]
        public void Constructor_WithNonPositiveFrameSampleRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VideoPlaybackTelemetry(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VideoPlaybackTelemetry(-1));
        }

        [Fact]
        public void Constructor_WithDefaultOrPositiveFrameSampleRate_Succeeds()
        {
            _ = new VideoPlaybackTelemetry();
            _ = new VideoPlaybackTelemetry(1);
            _ = new VideoPlaybackTelemetry(1000);
        }

        [Fact]
        public void EveryRecordMethod_IsCallableWithoutThrowing_RegardlessOfWhetherAnyoneIsListening()
        {
            var telemetry = new VideoPlaybackTelemetry();

            var exception = Record.Exception(() =>
            {
                foreach (var mediaType in new[] { VideoPlaybackMediaType.Video, VideoPlaybackMediaType.Audio })
                {
                    telemetry.RecordFrameDecoded(mediaType);
                    telemetry.RecordDecodeDuration(TimeSpan.FromMilliseconds(3), mediaType);
                    telemetry.RecordFrameEncoded(mediaType);
                    telemetry.RecordEncodeDuration(TimeSpan.FromMilliseconds(2), mediaType);
                    telemetry.RecordFramePublished(mediaType, payloadBytes: 128);
                    telemetry.RecordPublishLatency(TimeSpan.FromMilliseconds(1), mediaType);
                    telemetry.RecordPacingDrift(TimeSpan.FromMilliseconds(-5), mediaType);
                    telemetry.RecordFrameAcknowledged(mediaType);

                    foreach (FrameDropReason reason in Enum.GetValues<FrameDropReason>())
                        telemetry.RecordFrameDropped(reason, mediaType);
                }

                telemetry.RecordSubscriberJoined();
                telemetry.RecordSubscriberLeft();
                telemetry.RecordSessionFailure("open");
                telemetry.RecordSessionFailure("playback");
            });

            Assert.Null(exception);
        }

        // #235's own AC, "Sample or trace-gate per-frame logs": with nothing listening (this test host's
        // own state — see this type's own remarks), StartSampledFrameActivity must always short-circuit to
        // null rather than paying for an Activity nobody will ever collect.
        [Fact]
        public void StartSampledFrameActivity_WithNoListener_AlwaysReturnsNull()
        {
            var telemetry = new VideoPlaybackTelemetry(frameSampleRate: 1);

            for (var i = 0; i < 10; i++)
            {
                using var activity = telemetry.StartSampledFrameActivity(
                    VideoPlaybackMediaType.Video, "session", epoch: 1, frameNumber: i,
                    mediaPosition: TimeSpan.FromSeconds(i), pacingDrift: TimeSpan.Zero,
                    publishLatency: TimeSpan.FromMilliseconds(1), payloadBytes: 64);

                Assert.Null(activity);
            }
        }

        // #235's own AC, "Telemetry overhead is bounded ... verified by a focused load test." A generous
        // ceiling (not a tight budget) — this asserts recording stays cheap in aggregate, not that it hits
        // a specific number tied to whatever hardware happens to run CI.
        [Fact]
        public void RecordingManyFrames_StaysWithinABoundedOverheadBudget()
        {
            var telemetry = new VideoPlaybackTelemetry();
            const int iterations = 100_000;

            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                telemetry.RecordFrameDecoded(VideoPlaybackMediaType.Video);
                telemetry.RecordDecodeDuration(TimeSpan.FromMilliseconds(3), VideoPlaybackMediaType.Video);
                telemetry.RecordFrameEncoded(VideoPlaybackMediaType.Video);
                telemetry.RecordEncodeDuration(TimeSpan.FromMilliseconds(2), VideoPlaybackMediaType.Video);
                telemetry.RecordFramePublished(VideoPlaybackMediaType.Video, payloadBytes: 4096);
                telemetry.RecordPublishLatency(TimeSpan.FromMilliseconds(1), VideoPlaybackMediaType.Video);
                telemetry.RecordPacingDrift(TimeSpan.FromMilliseconds(0.5), VideoPlaybackMediaType.Video);
            }
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5),
                $"Recording {iterations} frames' worth of telemetry took {stopwatch.Elapsed}, which exceeds the bounded-overhead budget.");
        }
    }
}
