using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.BuildingBlocks.Application.Helpers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ThunderPropagator.BuildingBlocks.Infrastructure.SystemResourceMonitor;
using ThunderPropagator.Channels.ResourceMonitoring.Channel;
using ThunderPropagator.Channels.ResourceMonitoring.Feeders;
using ThunderPropagator.Channels.ResourceMonitoring.Messages;

namespace ThunderPropagator.Channels.ResourceMonitoring.Feeders
{
    internal
#if !DEBUG
        sealed
#endif
        class ResourceMonitoringChannelFeeder : IterativeFeeder<ResourceMonitoringChannel, ResourceMonitoringChannelFeederMessage, ResourceMonitoringChannelFeederConfiguration>
    {
        private sealed record AlertInfo(string Type, string Alert);

        // Task.Delay(TimeSpan) rejects delays beyond uint.MaxValue - 1 milliseconds; this is the
        // largest UtilizationWindow (in seconds) that converts to a millisecond value it can accept.
        internal const int MaxUtilizationWindowSeconds = (int)((uint.MaxValue - 1) / 1000);

        private readonly ResourceMonitoringChannelFeederConfiguration _feederConfiguration;
        private readonly ISystemResourceMonitor _resourceMonitor;
        private readonly long _window;

        // Issue #46 audit: node-local by design, not a candidate for cluster-shared state — dedupes
        // this node's own repeated alert against this node's own previous poll of its own
        // ISystemResourceMonitor (CPU/memory are per-machine OS state). Node A's resource alert has
        // no meaning applied to Node B's, so sharing this value across the cluster would be wrong,
        // not merely unnecessary — the same reasoning NetworkMonitoringChannelFeeder's own audit
        // comment gives for its byte counters.
        private string _lastAlert = "";

        // Tracks active subscriptions locally via the channel's public SubscriptionAdded/Removed
        // events, since neither is exposed to feeder code any other way. Read with Volatile.Read
        // and written with Interlocked so the poll loop always sees the latest count. Issue #46
        // audit: also node-local by design — it gates whether *this* node's feeder has any reason to
        // poll/emit at all, a per-node efficiency check, not state a subscriber on another node needs
        // visibility into.
        private int _activeSubscriptions;

        public ResourceMonitoringChannelFeeder(ResourceMonitoringChannel channel,
            ResourceMonitoringChannelFeederConfiguration feederConfiguration,
            IFeederHandler<ResourceMonitoringChannel, ResourceMonitoringChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            _feederConfiguration = feederConfiguration;
            _resourceMonitor = serviceProvider.GetRequiredService<ISystemResourceMonitor>();

            HealthName = nameof(ResourceMonitoringChannelFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];

            Guard.Against.OutOfRange(feederConfiguration.UtilizationWindow, nameof(feederConfiguration.UtilizationWindow), 1, MaxUtilizationWindowSeconds);
            _window = checked((long)feederConfiguration.UtilizationWindow * 1000L);

            channel.SubscriptionAdded += (_, _) => Interlocked.Increment(ref _activeSubscriptions);
            channel.SubscriptionRemoved += (_, _) => Interlocked.Decrement(ref _activeSubscriptions);
        }

        private string GetAlert(SystemResourceMonitorMetrics metrics)
        {
            List<AlertInfo> alerts = [];

            try
            {
                if (metrics.Memory.UsagePercentage > _feederConfiguration.MemoryUsedPercentageThreshold)
                    alerts.Add(new AlertInfo("Memory", $"Memory usage has exceeded the threshold of {_feederConfiguration.MemoryUsedPercentageThreshold}%. Please investigate immediately."));

                metrics.Drives
                    .Where(drive => drive.IsReady && drive.UsagePercentage > _feederConfiguration.StorageUsedPercentageThreshold)
                    .ToList()
                    .ForEach(drive =>
                        alerts.Add(new AlertInfo("Storage", $"Storage usage on <code>{drive.Letter}</code> has exceeded the threshold of {_feederConfiguration.StorageUsedPercentageThreshold}%. Please investigate immediately.")));

                return alerts.ToNJson();
            }
            finally
            {
                alerts.Clear();
            }
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<ResourceMonitoringChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(_window), cancellationToken);

            if (Volatile.Read(ref _activeSubscriptions) <= 0)
                yield break;

            var metrics = await _resourceMonitor.GetMetricsAsync(_window, true, cancellationToken);

            var sendAlert = false;
            var alert = GetAlert(metrics);
            if (!_lastAlert.Equals(alert))
            {
                sendAlert = true;
                _lastAlert = alert;
            }

            ResourceMonitoringChannelFeederMessage resourceMonitoringChannelFeederMessage = new()
            {
                Alert = sendAlert ? alert : null,
                DateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                MemoryUsedPercentage = metrics.Memory.UsagePercentage,
                MemoryUsedInBytes = metrics.Memory.Used,
                MaximumMemoryInBytes = metrics.Memory.Total,
                CpuUsedPercentage = metrics.Cpu.Usage,
                MaximumCpuUnits = metrics.Cpu.ProcessorCount,
                Processes = metrics.Cpu.Processes,
                Threads = metrics.Cpu.TotalThreads
            };

            yield return resourceMonitoringChannelFeederMessage;
        }
    }
}
