using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.BuildingBlocks.Application.Helpers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ThunderPropagator.BuildingBlocks.Infrastructure.SystemResourceMonitor;

namespace ThunderPropagator.Channels.ResourceMonitoring
{
    internal
#if !DEBUG
        sealed
#endif
        class ResourceMonitoringChannelFeeder : IterativeFeeder<ResourceMonitoringChannel, ResourceMonitoringChannelFeederMessage, ResourceMonitoringChannelFeederConfiguration>
    {
        private sealed record AlertInfo(string Type, string Alert);

        private readonly ResourceMonitoringChannelFeederConfiguration _feederConfiguration;
        private readonly ISystemResourceMonitor _resourceMonitor;
        private readonly long _window;
        private string _lastAlert = "";

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

            _window = feederConfiguration.UtilizationWindow * 1000;
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

            var metrics = _resourceMonitor.GetMetrics(_window, true);

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