using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// Issue #219's own ACs for the decode-to-publish side: bounded memory under sustained overload,
    /// playback advancing to the current media position rather than draining backlog, and dropped
    /// frame buffers disposed safely.
    /// </summary>
    public sealed class DecodedFrameBufferTests
    {
        private static DecodedVideoFrame CreateFrame(TimeSpan pts, out bool[] disposedFlag)
        {
            var flags = new bool[1];
            disposedFlag = flags;
            return new DecodedVideoFrame(pts, TimeSpan.FromMilliseconds(33), 4, 4, VideoPixelFormat.Bgra32, new byte[4 * 4 * 4], onDispose: () => flags[0] = true);
        }

        private static DecodedVideoFrame CreateFrame(TimeSpan pts) => CreateFrame(pts, out _);

        [Fact]
        public void Constructor_WithNonPositiveCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DecodedFrameBuffer(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DecodedFrameBuffer(-1));
        }

        [Fact]
        public void TryTakeCurrent_WithNothingDueYet_ReturnsFalse()
        {
            using var buffer = new DecodedFrameBuffer(4);
            buffer.Enqueue(CreateFrame(TimeSpan.FromSeconds(5)));

            var found = buffer.TryTakeCurrent(TimeSpan.FromSeconds(1), out var frame);

            Assert.False(found);
            Assert.Null(frame);
        }

        [Fact]
        public void TryTakeCurrent_ReturnsTheNewestDueFrame_AndDisposesOlderOnes()
        {
            using var buffer = new DecodedFrameBuffer(8);

            var frame1 = CreateFrame(TimeSpan.FromMilliseconds(0), out var frame1Disposed);
            var frame2 = CreateFrame(TimeSpan.FromMilliseconds(33), out var frame2Disposed);
            var frame3 = CreateFrame(TimeSpan.FromMilliseconds(66), out var frame3Disposed);
            buffer.Enqueue(frame1);
            buffer.Enqueue(frame2);
            buffer.Enqueue(frame3);

            var found = buffer.TryTakeCurrent(TimeSpan.FromMilliseconds(50), out var current);

            Assert.True(found);
            Assert.Same(frame2, current);
            Assert.True(frame1Disposed[0], "the superseded earlier frame must be disposed");
            Assert.True(frame2Disposed[0] == false, "the returned frame must not itself be disposed by the buffer");
            Assert.False(frame3Disposed[0], "a frame still in the future must be left untouched");
        }

        [Fact]
        public void TryTakeCurrent_LeavesFutureFramesForALaterCall()
        {
            using var buffer = new DecodedFrameBuffer(8);
            buffer.Enqueue(CreateFrame(TimeSpan.FromMilliseconds(0)));
            buffer.Enqueue(CreateFrame(TimeSpan.FromMilliseconds(100)));

            buffer.TryTakeCurrent(TimeSpan.FromMilliseconds(10), out _);

            Assert.Equal(1, buffer.Count);

            var found = buffer.TryTakeCurrent(TimeSpan.FromMilliseconds(150), out var frame);
            Assert.True(found);
            Assert.Equal(TimeSpan.FromMilliseconds(100), frame!.PresentationTimestamp);
        }

        [Fact]
        public void Enqueue_WhenAtCapacity_DisposesTheOldestFrameAndStaysBounded()
        {
            var dropReasons = new List<FrameDropReason>();
            using var buffer = new DecodedFrameBuffer(3, dropReasons.Add);

            var oldest = CreateFrame(TimeSpan.Zero, out var oldestDisposed);
            buffer.Enqueue(oldest);
            buffer.Enqueue(CreateFrame(TimeSpan.FromMilliseconds(33)));
            buffer.Enqueue(CreateFrame(TimeSpan.FromMilliseconds(66)));

            Assert.Equal(3, buffer.Count);

            buffer.Enqueue(CreateFrame(TimeSpan.FromMilliseconds(99)));

            Assert.Equal(3, buffer.Count); // never exceeds capacity
            Assert.True(oldestDisposed[0]);
            Assert.Equal([FrameDropReason.DecodeBufferCapacityExceeded], dropReasons);
        }

        [Fact]
        public void SustainedOverload_QueueNeverExceedsCapacity_AndEveryDroppedFrameIsDisposedExactlyOnce()
        {
            const int capacity = 16;
            var disposedCount = 0;

            const int iterations = 5_000;
            using var buffer = new DecodedFrameBuffer(capacity, _ => { });

            for (var i = 0; i < iterations; i++)
            {
                var pts = TimeSpan.FromMilliseconds(i * 33);
                buffer.Enqueue(new DecodedVideoFrame(pts, TimeSpan.FromMilliseconds(33), 4, 4, VideoPixelFormat.Bgra32, new byte[16], onDispose: () => Interlocked.Increment(ref disposedCount)));

                Assert.True(buffer.Count <= capacity);
            }

            Assert.Equal(iterations - capacity, disposedCount);
        }

        [Fact]
        public void Dispose_DisposesEveryRemainingBufferedFrame()
        {
            var flags = new bool[3];
            var buffer = new DecodedFrameBuffer(8);
            buffer.Enqueue(new DecodedVideoFrame(TimeSpan.Zero, TimeSpan.Zero, 4, 4, VideoPixelFormat.Bgra32, new byte[16], () => flags[0] = true));
            buffer.Enqueue(new DecodedVideoFrame(TimeSpan.FromMilliseconds(33), TimeSpan.Zero, 4, 4, VideoPixelFormat.Bgra32, new byte[16], () => flags[1] = true));
            buffer.Enqueue(new DecodedVideoFrame(TimeSpan.FromMilliseconds(66), TimeSpan.Zero, 4, 4, VideoPixelFormat.Bgra32, new byte[16], () => flags[2] = true));

            buffer.Dispose();

            Assert.All(flags, Assert.True);
        }

        [Fact]
        public void Dispose_IsSafeToCallMoreThanOnce()
        {
            var buffer = new DecodedFrameBuffer(4);
            buffer.Enqueue(CreateFrame(TimeSpan.Zero));

            buffer.Dispose();
            var exception = Record.Exception(buffer.Dispose);

            Assert.Null(exception);
        }

        [Fact]
        public void Enqueue_AfterDispose_Throws()
        {
            var buffer = new DecodedFrameBuffer(4);
            buffer.Dispose();

            Assert.Throws<ObjectDisposedException>(() => buffer.Enqueue(CreateFrame(TimeSpan.Zero)));
        }

        // Issue #219's own AC: "Stress tests cover slow decode, slow publish, and slow-subscriber
        // scenarios." Slow decode: the consumer drains faster than frames arrive — nothing should ever
        // need to be dropped.
        [Fact]
        public void SlowDecode_ConsumerDrainingFasterThanProduction_NeverDropsAnything()
        {
            var dropReasons = new List<FrameDropReason>();
            using var buffer = new DecodedFrameBuffer(4, dropReasons.Add);

            for (var i = 0; i < 200; i++)
            {
                var pts = TimeSpan.FromMilliseconds(i * 33);
                buffer.Enqueue(CreateFrame(pts));

                var found = buffer.TryTakeCurrent(pts, out var frame);
                Assert.True(found);
                Assert.Equal(pts, frame!.PresentationTimestamp);
                frame.Dispose();
            }

            Assert.Empty(dropReasons);
        }

        // Slow publish: production far outpaces consumption — capacity must hold regardless of how much
        // backlog would otherwise accumulate, and calling TryTakeCurrent once must skip straight to
        // whatever is due "now," not replay the whole backlog frame by frame.
        [Fact]
        public void SlowPublish_ProductionFarOutpacingConsumption_StaysBoundedAndCatchesUpToLive()
        {
            const int capacity = 8;
            const int iterations = 2_000;
            using var buffer = new DecodedFrameBuffer(capacity);

            for (var i = 0; i < iterations; i++)
                buffer.Enqueue(CreateFrame(TimeSpan.FromMilliseconds(i * 33)));

            Assert.True(buffer.Count <= capacity);

            var latestPts = TimeSpan.FromMilliseconds((iterations - 1) * 33);
            var found = buffer.TryTakeCurrent(latestPts, out var frame);

            Assert.True(found);
            Assert.Equal(latestPts, frame!.PresentationTimestamp); // caught up to live in one call
            Assert.Equal(0, buffer.Count);
        }
    }
}
