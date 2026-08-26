using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using ThunderPropagator.Channels.Throughput.Feeders;

namespace ThunderPropagator.UnitTests.Channels.Throughput
{
    /// <summary>
    /// ThroughputChannelFeeder relies on MetricCollector&lt;long&gt; to safely observe measurements
    /// recorded by producers on other threads while it periodically snapshots and resets. These tests
    /// exercise that exact pattern directly against MetricCollector&lt;long&gt; — concurrent producers
    /// calling Counter.Add while snapshots are repeatedly taken and reset — to confirm no measurement is
    /// lost or double-counted across a snapshot/reset boundary. A private Meter is used so the stress
    /// producers can't be polluted by unrelated code recording against the app's shared static counters.
    /// </summary>
    public sealed class ThroughputMetricCollectorConcurrencyTests
    {
        [Fact]
        public async Task GetMeasurementSnapshot_ConcurrentProducersWithRepeatedResets_AccountsForEveryMeasurementExactlyOnce()
        {
            const int producerCount = 8;
            const int measurementsPerProducer = 5_000;
            const long expectedTotal = producerCount * measurementsPerProducer;

            using var meter = new Meter(Guid.NewGuid().ToString("N"));
            var counter = meter.CreateCounter<long>("test.throughput.stress.counter");
            using var collector = new MetricCollector<long>(counter);

            long collectedTotal = 0;
            using var producersDone = new CancellationTokenSource();

            var snapshotLoop = Task.Run(async () =>
            {
                while (!producersDone.IsCancellationRequested)
                {
                    var snapshot = collector.GetMeasurementSnapshot(clear: true);
                    Interlocked.Add(ref collectedTotal, snapshot.Sum(measurement => measurement.Value));
                    await Task.Yield();
                }
            });

            var producers = Enumerable.Range(0, producerCount)
                .Select(_ => Task.Run(() =>
                {
                    for (var i = 0; i < measurementsPerProducer; i++)
                        counter.Add(1);
                }))
                .ToArray();

            await Task.WhenAll(producers);
            await producersDone.CancelAsync();
            await snapshotLoop;

            // Catch whatever landed between the snapshot loop's last iteration and cancellation.
            var finalSnapshot = collector.GetMeasurementSnapshot(clear: true);
            Interlocked.Add(ref collectedTotal, finalSnapshot.Sum(measurement => measurement.Value));

            Assert.Equal(expectedTotal, Interlocked.Read(ref collectedTotal));
        }
    }
}
