using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Application.Feeders;
using RapidStreamer.BuildingBlocks.Application.Helpers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using RapidStreamer.BuildingBlocks.Infrastructure.SystemResourceMonitor;

namespace RapidStreamer.Channels.ResourceMonitoring
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
                if (metrics.MemoryMetrics.UsagePercentage > _feederConfiguration.MemoryUsedPercentageThreshold)
                    alerts.Add(new AlertInfo("Memory", $"Memory usage has exceeded the threshold of {_feederConfiguration.MemoryUsedPercentageThreshold}%. Please investigate immediately."));

                metrics.SystemDrives
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

            var processes = Process.GetProcesses();
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
                MemoryUsedPercentage = metrics.MemoryMetrics.UsagePercentage,
                MemoryUsedInBytes = metrics.MemoryMetrics.Used,
                MaximumMemoryInBytes = metrics.MemoryMetrics.Total,
                CpuUsedPercentage = metrics.CpuMetrics.Usage,
                MaximumCpuUnits = metrics.CpuMetrics.ProcessorCount,
                Processes = processes.Length,
                Threads = processes.Sum(x => x.Threads.Count)
            };

            yield return resourceMonitoringChannelFeederMessage;
        }
    }
}