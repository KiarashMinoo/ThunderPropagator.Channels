using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.ResourceMonitoring
{
    public
#if !DEBUG
        sealed
#endif
        class ResourceMonitoringChannelConfiguration : AbstractChannelConfiguration
    {
        public ResourceMonitoringChannelFeederConfiguration FeederConfiguration { get; set; } = new();

        public ResourceMonitoringChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}