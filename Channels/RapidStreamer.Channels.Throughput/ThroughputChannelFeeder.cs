using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using RapidStreamer.Application.Feeders;
using RapidStreamer.Application.Metrics;
using System.Runtime.CompilerServices;

namespace RapidStreamer.Channels.Throughput
{
    internal
#if !DEBUG
        sealed
#endif
        class ThroughputChannelFeeder : IterativeFeeder<ThroughputChannel, ThroughputChannelFeederMessage, ThroughputChannelFeederConfiguration>
    {
        private readonly MetricCollector<long> _feedersHandledMetricCollector;
        private readonly MetricCollector<long> _feedersHandledDurationMetricCollector;
        private readonly MetricCollector<long> _pushedMessageMetricCollector;
        private readonly MetricCollector<long> _pushedMessageSizeMetricCollector;

        public ThroughputChannelFeeder(ThroughputChannel channel,
            ThroughputChannelFeederConfiguration feederConfiguration,
            IFeederHandler<ThroughputChannel, ThroughputChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            HealthName = nameof(ThroughputChannelFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];

            _feedersHandledMetricCollector = new MetricCollector<long>
                (FeedersTelemetry.FeedersHandledCounter.Meter.Scope, FeedersTelemetry.FeedersHandledCounter.Meter.Name, FeedersTelemetry.FeedersHandledCounter.Name);

            _feedersHandledDurationMetricCollector = new MetricCollector<long>
            (FeedersTelemetry.FeedersHandledDurationHistogram.Meter.Scope, FeedersTelemetry.FeedersHandledDurationHistogram.Meter.Name,
                FeedersTelemetry.FeedersHandledDurationHistogram.Name);

            _pushedMessageMetricCollector = new MetricCollector<long>
                (PushedMessageTelemetry.PushedMessageCounter.Meter.Scope, PushedMessageTelemetry.PushedMessageCounter.Meter.Name, PushedMessageTelemetry.PushedMessageCounter.Name);

            _pushedMessageSizeMetricCollector = new MetricCollector<long>
            (PushedMessageTelemetry.PushedMessageSizeHistogram.Meter.Scope, PushedMessageTelemetry.PushedMessageSizeHistogram.Meter.Name,
                PushedMessageTelemetry.PushedMessageSizeHistogram.Name);
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<ThroughputChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            var feedersHandledMeasurementSnapshot = _feedersHandledMetricCollector.GetMeasurementSnapshot(true);
            var feedersHandledDurationMeasurementSnapshot = _feedersHandledDurationMetricCollector.GetMeasurementSnapshot(true);
            var pushedMessageMeasurementSnapshot = _pushedMessageMetricCollector.GetMeasurementSnapshot(true);
            var pushedMessageSizeMeasurementSnapshot = _pushedMessageSizeMetricCollector.GetMeasurementSnapshot(true);

            yield return new ThroughputChannelFeederMessage
            {
                UpStreamHandled = feedersHandledMeasurementSnapshot.Count,
                DownStreamHandled = pushedMessageMeasurementSnapshot.Count,
                DownStreamSize = feedersHandledDurationMeasurementSnapshot.Count > 0 ? feedersHandledDurationMeasurementSnapshot.Average(x => x.Value) : 0,
                DownStreamDuration = pushedMessageSizeMeasurementSnapshot.Count > 0 ? pushedMessageSizeMeasurementSnapshot.Sum(x => x.Value) : 0
            };
        }
    }
}