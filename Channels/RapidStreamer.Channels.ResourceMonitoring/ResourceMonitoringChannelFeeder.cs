using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using RapidStreamer.Application.Feeders;
using RapidStreamer.BuildingBlocks.Application.Helpers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        private readonly IResourceMonitor _resourceMonitor;
        private readonly TimeSpan _window;
        private string _lastAlert = "";

        public ResourceMonitoringChannelFeeder(ResourceMonitoringChannel channel,
            ResourceMonitoringChannelFeederConfiguration feederConfiguration,
            IFeederHandler<ResourceMonitoringChannel, ResourceMonitoringChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            _feederConfiguration = feederConfiguration;
            _resourceMonitor = serviceProvider.GetRequiredService<IResourceMonitor>();

            HealthName = nameof(ResourceMonitoringChannelFeeder);
            HealthTags = [.. HealthTags, "StaticFeeder"];

            _window = TimeSpan.FromSeconds(feederConfiguration.UtilizationWindow);
        }

        private string GetAlert(ResourceUtilization utilization)
        {
            List<AlertInfo> alerts = [];
            try
            {
                if (utilization.MemoryUsedPercentage > _feederConfiguration.MemoryUsedPercentageThreshold)
                    alerts.Add(new AlertInfo("Memory",
                        $"Memory usage has exceeded the threshold of {_feederConfiguration.MemoryUsedPercentageThreshold}%. Please investigate immediately."));

                DriveInfo.GetDrives().Where(drive => drive.IsReady).Select(drive => new
                {
                    Drive = drive,
                    Usage = 100.0 - ((1.0 * drive.TotalFreeSpace / drive.TotalSize) * 100)
                }).Where(x => x.Usage > _feederConfiguration.StorageUsedPercentageThreshold).ToArray().ForEach(drive => alerts.Add(new AlertInfo("Storage",
                    $"Storage usage on <code>{drive.Drive.Name}</code> has exceeded the threshold of {_feederConfiguration.StorageUsedPercentageThreshold}%. Please investigate immediately.")));

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
            await Task.Delay(_window, cancellationToken);

            var processes = Process.GetProcesses();
            var utilization = _resourceMonitor.GetUtilization(_window);
            var resources = utilization.SystemResources;

            var sendAlert = false;
            var alert = GetAlert(utilization);
            if (!_lastAlert.Equals(alert))
            {
                sendAlert = true;
                _lastAlert = alert;
            }

            ResourceMonitoringChannelFeederMessage resourceMonitoringChannelFeederMessage = new()
            {
                Alert = sendAlert ? alert : null,
                DateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                CpuUsedPercentage = (utilization.CpuUsedPercentage / resources.GuaranteedCpuUnits) / _window.TotalSeconds,
                MemoryUsedPercentage = utilization.MemoryUsedPercentage,
                MemoryUsedInBytes = utilization.MemoryUsedInBytes,
                GuaranteedMemoryInBytes = resources.GuaranteedMemoryInBytes,
                MaximumMemoryInBytes = resources.MaximumMemoryInBytes,
                GuaranteedCpuUnits = resources.GuaranteedCpuUnits,
                MaximumCpuUnits = resources.MaximumCpuUnits,
                Processes = processes.Length,
                Threads = processes.Sum(x => x.Threads.Count)
            };

            yield return resourceMonitoringChannelFeederMessage;
        }
    }
}