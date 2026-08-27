using ThunderPropagator.Channels.Demo.VideoPlayer;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;
using ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Session
{
    /// <summary>
    /// #236's own scope, "one slow subscriber does not block another" — <see cref="SubscriberFrameQueueTests"/>
    /// already proves this at the queue-primitive level (two independent <see cref="SubscriberFrameQueue{T}"/>
    /// instances never interact); this proves the same property end to end through
    /// <see cref="VideoPlaybackSession"/>'s own real publish fan-out (<c>PublishFrame</c>'s
    /// <c>foreach (var subscriber in _subscribers.Values)</c> loop), which is what actually delivers frames
    /// to more than one viewer at once.
    /// </summary>
    public sealed class VideoPlaybackSessionBackpressureTests
    {
        private static readonly VideoSource TestSource = new() { Location = "synthetic://test" };

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
        public async Task PublishFrame_OneSubscriberNeverDrained_StillDeliversFreshFramesToAnActivelyDrainingSubscriber()
        {
            // A deliberately tiny SubscriberQueueCapacity: "slow" fills it almost immediately and stays
            // full/dropping for the rest of the run, which is exactly the condition this test needs to
            // prove never leaks into "fast"'s own delivery. PlaybackRate 4.0 (not an extreme
            // FastOptions-style acceleration) so "fast" has real opportunity to drain across more than one
            // WaitUntilAsync poll — mirrors VideoPlaybackSessionTests' own established real-time-but-bounded
            // pattern for tests that need genuine mid-stream polling.
            var options = new VideoPlaybackSessionOptions
            {
                SubscriberQueueCapacity = 2,
                PlaybackRate = 4.0,
                PollInterval = TimeSpan.FromMilliseconds(2)
            };

            await using var session = new VideoPlaybackSession(
                "backpressure-1", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughEncode);

            session.Subscribe("fast");
            session.Subscribe("slow"); // never dequeued for the whole test — stays full/dropping throughout

            await session.SelectAsync(TestSource);

            var fastFrameNumbers = new List<long>();
            await WaitUntilAsync(() =>
            {
                while (session.TryDequeue("fast", out var packet))
                    fastFrameNumbers.Add(packet!.FrameNumber);

                return session.State == PlayState.Ended;
            }, TimeSpan.FromSeconds(10));

            while (session.TryDequeue("fast", out var packet)) // drain whatever published after the last poll
                fastFrameNumbers.Add(packet!.FrameNumber);

            // SyntheticVideoFrameSource produces 10 frames total. "fast" was actively drained throughout,
            // so seeing more than its own SubscriberQueueCapacity worth of frames proves delivery to it
            // kept flowing normally rather than ever stalling on "slow"'s own permanently-full queue.
            Assert.True(fastFrameNumbers.Count > options.SubscriberQueueCapacity,
                $"expected \"fast\" to receive more than {options.SubscriberQueueCapacity} frames since it was actively drained throughout, but got {fastFrameNumbers.Count}.");
            Assert.Equal(fastFrameNumbers, fastFrameNumbers.OrderBy(n => n));
        }

        [Fact]
        public async Task PublishFrame_OneSubscriberNeverDrained_ItsOwnQueueStaysBounded_RatherThanGrowingOrBlockingThePublisher()
        {
            var options = new VideoPlaybackSessionOptions
            {
                SubscriberQueueCapacity = 2,
                PlaybackRate = 100_000, // FastOptions-style: this run only needs to reach Ended, not be caught mid-stream
                PollInterval = TimeSpan.FromMilliseconds(2)
            };

            await using var session = new VideoPlaybackSession(
                "backpressure-2", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughEncode);

            session.Subscribe("slow"); // never dequeued

            await session.SelectAsync(TestSource);

            // If a full, never-drained subscriber queue could ever block PublishFrame's own fan-out loop,
            // this playback would never reach Ended — SubscriberFrameQueue.Enqueue is documented as never
            // blocking, and this is the end-to-end proof of that through the real publish path.
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            var slowFrames = new List<VideoFramePacket>();
            while (session.TryDequeue("slow", out var packet))
                slowFrames.Add(packet!);

            Assert.True(slowFrames.Count <= options.SubscriberQueueCapacity);
        }
    }
}
