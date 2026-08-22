using System.Diagnostics.Metrics;
using ThunderPropagator.Channels.Chat.Pipelines;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    /// <summary>
    /// Issue #139: every Chat receiver pipeline used to lazily create its request counter with
    /// `_counter ??= Telemetry.CreateCounter&lt;long&gt;(...)`, a non-atomic check-then-assign that
    /// concurrent first use could race — potentially creating more than one Counter&lt;long&gt;
    /// instrument before one wins. ChatChannelPipelineTelemetry.EnsureCounter is the single helper
    /// every pipeline now routes through instead; these tests exercise it directly, since a
    /// pipeline's own Invoke can't be driven in isolation (ChannelInfo's constructor is internal to a
    /// closed-source assembly — see ChatChannelAuthenticationTests' own comment).
    /// </summary>
    public sealed class ChatChannelPipelineTelemetryTests
    {
        [Fact]
        public void EnsureCounter_ConcurrentFirstUse_CreatesTheCounterExactlyOnce()
        {
            const int threadCount = 16;

            using var meter = new Meter(Guid.NewGuid().ToString("N"));
            Counter<long>? counter = null;
            var counterLock = new object();
            var creationCount = 0;
            using var barrier = new Barrier(threadCount);
            var results = new Counter<long>?[threadCount];

            // Dedicated threads rather than pooled Tasks: ThreadPool's throttled thread-injection
            // rate can take several seconds to ramp up to threadCount workers, which would make this
            // test slow without adding anything to what it proves.
            var threads = Enumerable.Range(0, threadCount)
                .Select(i => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    results[i] = ChatChannelPipelineTelemetry.EnsureCounter(ref counter, counterLock, () =>
                    {
                        Interlocked.Increment(ref creationCount);
                        return meter.CreateCounter<long>("test.chat.pipeline.counter");
                    });
                }))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(1, creationCount);
            Assert.All(results, result => Assert.Same(results[0], result));
        }

        [Fact]
        public void EnsureCounter_WhenAlreadySet_ReturnsTheExistingInstanceWithoutCallingTheFactory()
        {
            using var meter = new Meter(Guid.NewGuid().ToString("N"));
            var existing = meter.CreateCounter<long>("test.chat.pipeline.counter.existing");
            Counter<long>? counter = existing;
            var counterLock = new object();
            var factoryCalled = false;

            var result = ChatChannelPipelineTelemetry.EnsureCounter(ref counter, counterLock, () =>
            {
                factoryCalled = true;
                return meter.CreateCounter<long>("should-not-be-created");
            });

            Assert.Same(existing, result);
            Assert.False(factoryCalled);
        }

        [Fact]
        public void EnsureCounter_FirstCall_CreatesAndStoresTheCounter()
        {
            using var meter = new Meter(Guid.NewGuid().ToString("N"));
            Counter<long>? counter = null;
            var counterLock = new object();
            var created = meter.CreateCounter<long>("test.chat.pipeline.counter.first");

            var result = ChatChannelPipelineTelemetry.EnsureCounter(ref counter, counterLock, () => created);

            Assert.Same(created, result);
            Assert.Same(created, counter);
        }
    }
}
