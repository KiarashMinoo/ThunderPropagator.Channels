using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Application.Metrics;
using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Throughput.Channel;
using ThunderPropagator.Channels.Throughput.Feeders;
using ThunderPropagator.Channels.Throughput.Messages;

namespace ThunderPropagator.Channels.Throughput.Feeders
{
    internal
#if !DEBUG
        sealed
#endif
        class ThroughputChannelFeeder : IterativeFeeder<ThroughputChannel, ThroughputChannelFeederMessage, ThroughputChannelFeederConfiguration>
    {
        // Concurrency model: each MetricCollector<long> below guards its internal measurement list with
        // a private lock shared between the write path (OnMeasurementRecorded, invoked synchronously by
        // the .NET Meter/MeterListener machinery on whichever thread calls Counter.Add/Histogram.Record)
        // and GetMeasurementSnapshot(clear: true). Snapshot-and-clear is therefore atomic and mutually
        // exclusive with concurrent measurement delivery — a producer thread recording a measurement
        // either lands entirely before or entirely after a given snapshot, never torn, lost, or counted
        // twice. This feeder owns no additional synchronization because none is needed: every producer
        // (elsewhere in the app, on arbitrary threads) only ever calls Counter.Add/Histogram.Record, and
        // this feeder is the sole reader/resetter of each collector, called sequentially from its own
        // single-threaded ReceiveAsync iteration.
        private readonly MetricCollector<long>? _feedersHandledMetricCollector;
        private readonly MetricCollector<long>? _feedersHandledDurationMetricCollector;
        private readonly MetricCollector<long>? _pushedMessageMetricCollector;
        private readonly MetricCollector<long>? _pushedMessageSizeMetricCollector;

        // Tracks active subscriptions locally via the channel's public SubscriptionAdded/Removed
        // events, since neither is exposed to feeder code any other way. Read with Volatile.Read
        // and written with Interlocked so the poll loop always sees the latest count.
        private int _activeSubscriptions;

        public ThroughputChannelFeeder(ThroughputChannel channel,
            ThroughputChannelFeederConfiguration feederConfiguration,
            IFeederHandler<ThroughputChannel, ThroughputChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            HealthName = nameof(ThroughputChannelFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];

            channel.SubscriptionAdded += (_, _) => Interlocked.Increment(ref _activeSubscriptions);
            channel.SubscriptionRemoved += (_, _) => Interlocked.Decrement(ref _activeSubscriptions);

            if (FeedersTelemetry.FeedersHandledCounter is not null)
            {
                _feedersHandledMetricCollector = new MetricCollector<long>(
                    FeedersTelemetry.FeedersHandledCounter.Meter.Scope,
                    FeedersTelemetry.FeedersHandledCounter.Meter.Name,
                    FeedersTelemetry.FeedersHandledCounter.Name);
            }

            if (FeedersTelemetry.FeedersHandledDurationHistogram is not null)
            {
                _feedersHandledDurationMetricCollector = new MetricCollector<long>(
                    FeedersTelemetry.FeedersHandledDurationHistogram.Meter.Scope,
                    FeedersTelemetry.FeedersHandledDurationHistogram.Meter.Name,
                    FeedersTelemetry.FeedersHandledDurationHistogram.Name);
            }

            if (PushedMessageTelemetry.PushedMessageCounter is not null)
            {
                _pushedMessageMetricCollector = new MetricCollector<long>(
                    PushedMessageTelemetry.PushedMessageCounter.Meter.Scope,
                    PushedMessageTelemetry.PushedMessageCounter.Meter.Name,
                    PushedMessageTelemetry.PushedMessageCounter.Name);
            }

            if (PushedMessageTelemetry.PushedMessageSizeHistogram is not null)
            {
                _pushedMessageSizeMetricCollector = new MetricCollector<long>(
                    PushedMessageTelemetry.PushedMessageSizeHistogram.Meter.Scope,
                    PushedMessageTelemetry.PushedMessageSizeHistogram.Meter.Name,
                    PushedMessageTelemetry.PushedMessageSizeHistogram.Name);
            }
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<ThroughputChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            // Snapshots are drained unconditionally, subscribed or not, so each MetricCollector's
            // internal list keeps getting reset and never grows unboundedly. Only the aggregation
            // (Average/Sum) and the resulting message are skipped when nobody can observe them.
            var feedersHandledMeasurementSnapshot = _feedersHandledMetricCollector?.GetMeasurementSnapshot(true);
            var feedersHandledDurationMeasurementSnapshot = _feedersHandledDurationMetricCollector?.GetMeasurementSnapshot(true);
            var pushedMessageMeasurementSnapshot = _pushedMessageMetricCollector?.GetMeasurementSnapshot(true);
            var pushedMessageSizeMeasurementSnapshot = _pushedMessageSizeMetricCollector?.GetMeasurementSnapshot(true);

            if (Volatile.Read(ref _activeSubscriptions) <= 0)
                yield break;

            yield return new ThroughputChannelFeederMessage
            {
                UpStreamHandled = feedersHandledMeasurementSnapshot?.Count ?? 0,
                DownStreamHandled = pushedMessageMeasurementSnapshot?.Count ?? 0,
                DownStreamSize = feedersHandledDurationMeasurementSnapshot?.Count > 0 ? feedersHandledDurationMeasurementSnapshot.Average(x => x.Value) : 0,
                DownStreamDuration = pushedMessageSizeMeasurementSnapshot?.Count > 0 ? pushedMessageSizeMeasurementSnapshot.Sum(x => x.Value) : 0
            };
        }
    }
}