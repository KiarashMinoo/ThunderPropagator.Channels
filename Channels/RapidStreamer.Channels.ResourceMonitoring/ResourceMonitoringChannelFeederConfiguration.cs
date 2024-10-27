using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.ResourceMonitoring
{
    internal
#if !DEBUG
        sealed
#endif
        class ResourceMonitoringChannelFeederConfiguration : AbstractFeederConfiguration
    {
        public int UtilizationWindow { get; set; } = 1;
        public sbyte MemoryUsedPercentageThreshold { get; set; } = 80;
        public sbyte StorageUsedPercentageThreshold { get; set; } = 80;
    }
}