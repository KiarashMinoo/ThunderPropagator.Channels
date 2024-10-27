using RapidStreamer.BuildingBlocks.Application;

namespace RapidStreamer.Channels.ResourceMonitoring
{
    internal
#if !DEBUG
        sealed
#endif
        class ResourceMonitoringChannelFeederMessage : FeederMessage
    {
        public string Key
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public string? Alert
        {
            get => GetValueOrDefault<string?>(null);
            set => SetValue(value);
        }

        public string DateTime
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public double CpuUsedPercentage
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public double MemoryUsedPercentage
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public ulong MemoryUsedInBytes
        {
            get => GetValueOrDefault(0UL);
            set => SetValue(value);
        }

        public ulong GuaranteedMemoryInBytes
        {
            get => GetValueOrDefault(0UL);
            set => SetValue(value);
        }

        public ulong MaximumMemoryInBytes
        {
            get => GetValueOrDefault(0UL);
            set => SetValue(value);
        }

        public double GuaranteedCpuUnits
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public double MaximumCpuUnits
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public decimal Processes
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public decimal Threads
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public ResourceMonitoringChannelFeederMessage()
        {
            Key = nameof(ResourceMonitoring);
        }
    }
}