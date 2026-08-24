using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Issue #219's own ACs for the publish/subscriber side: a slow subscriber does not increase
    /// latency for healthy subscribers, and dropped items are disposed safely.
    /// </summary>
    public sealed class SubscriberFrameQueueTests
    {
        [Fact]
        public void Constructor_WithNonPositiveCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SubscriberFrameQueue<int>(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SubscriberFrameQueue<int>(-1));
        }

        [Fact]
        public void Enqueue_ThenDequeue_RoundTripsInFifoOrder()
        {
            using var queue = new SubscriberFrameQueue<int>(4);
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            Assert.True(queue.TryDequeue(out var first));
            Assert.True(queue.TryDequeue(out var second));
            Assert.Equal(1, first);
            Assert.Equal(2, second);
        }

        [Fact]
        public void TryDequeue_WhenEmpty_ReturnsFalse()
        {
            using var queue = new SubscriberFrameQueue<int>(4);

            Assert.False(queue.TryDequeue(out _));
        }

        [Fact]
        public void Enqueue_WhenAtCapacity_DropsOldestItem_AndReportsWhy()
        {
            var dropped = new List<int>();
            var reasons = new List<FrameDropReason>();
            using var queue = new SubscriberFrameQueue<int>(2, dropped.Add, reasons.Add);

            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3); // 1 is now the oldest and gets dropped

            Assert.Equal(2, queue.Count);
            Assert.Equal([1], dropped);
            Assert.Equal([FrameDropReason.SubscriberQueueCapacityExceeded], reasons);

            queue.TryDequeue(out var remaining1);
            queue.TryDequeue(out var remaining2);
            Assert.Equal(2, remaining1);
            Assert.Equal(3, remaining2);
        }

        [Fact]
        public void Dispose_DiscardsRemainingItemsThroughTheDropCallback()
        {
            var dropped = new List<string>();
            var queue = new SubscriberFrameQueue<string>(4, dropped.Add);
            queue.Enqueue("a");
            queue.Enqueue("b");

            queue.Dispose();

            Assert.Equal(["a", "b"], dropped);
        }

        [Fact]
        public void Dispose_IsSafeToCallMoreThanOnce()
        {
            var queue = new SubscriberFrameQueue<int>(4);
            queue.Enqueue(1);

            queue.Dispose();
            var exception = Record.Exception(queue.Dispose);

            Assert.Null(exception);
        }

        [Fact]
        public void Enqueue_AfterDispose_Throws()
        {
            var queue = new SubscriberFrameQueue<int>(4);
            queue.Dispose();

            Assert.Throws<ObjectDisposedException>(() => queue.Enqueue(1));
        }

        // Issue #219's own AC: "A slow subscriber does not increase latency for healthy subscribers."
        // Each instance owns its own lock and state entirely independently, so a slow subscriber's own
        // full queue structurally cannot affect a different instance at all — proven here by driving one
        // "slow" (never drained) queue deep into sustained overflow while a separate "healthy" queue is
        // used normally, and confirming the healthy one's behavior is entirely unaffected.
        [Fact]
        public void SlowSubscriber_SustainedOverflowOnOneQueue_NeverAffectsASeparateQueue()
        {
            const int iterations = 5_000;
            var slowDropCount = 0;
            using var slowSubscriber = new SubscriberFrameQueue<int>(4, _ => Interlocked.Increment(ref slowDropCount));
            using var healthySubscriber = new SubscriberFrameQueue<int>(4);

            for (var i = 0; i < iterations; i++)
            {
                slowSubscriber.Enqueue(i); // never drained — simulates a stalled subscriber

                healthySubscriber.Enqueue(i);
                Assert.True(healthySubscriber.TryDequeue(out var received));
                Assert.Equal(i, received);
            }

            Assert.True(slowSubscriber.Count <= slowSubscriber.Capacity);
            Assert.Equal(iterations - slowSubscriber.Capacity, slowDropCount);
            Assert.Equal(0, healthySubscriber.Count); // fully drained every time, unaffected by the other queue's overload
        }

        // Slow publish, from the subscriber's own point of view: the publisher enqueues far faster than
        // this one subscriber (still) drains — its queue must stay bounded rather than growing without
        // limit, and once it does start draining it must still get its most recent items, not a stale
        // backlog.
        [Fact]
        public void SlowPublish_ProductionFarOutpacingThisSubscribersOwnConsumption_StaysBoundedAndRecoversToRecentItems()
        {
            const int capacity = 8;
            const int iterations = 2_000;
            using var queue = new SubscriberFrameQueue<int>(capacity);

            for (var i = 0; i < iterations; i++)
                queue.Enqueue(i);

            Assert.Equal(capacity, queue.Count);

            queue.TryDequeue(out var oldestRemaining);
            Assert.Equal(iterations - capacity, oldestRemaining);
        }
    }
}
