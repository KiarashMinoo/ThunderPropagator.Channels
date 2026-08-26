using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.ResourceMonitoring.Feeders
{
    public
#if !DEBUG
        sealed
#endif
        class ResourceMonitoringChannelFeederConfiguration : AbstractFeederConfiguration
    {
        public int UtilizationWindow { get; set; } = 1;
        public sbyte MemoryUsedPercentageThreshold { get; set; } = 80;
        public sbyte StorageUsedPercentageThreshold { get; set; } = 80;

        public ResourceMonitoringChannelFeederConfiguration()
        {
            IsEnabled = true;
        }

        internal void Bind(ResourceMonitoringChannelFeederConfiguration resourceMonitoringChannelFeederConfiguration) => base.Bind(resourceMonitoringChannelFeederConfiguration);
    }
}