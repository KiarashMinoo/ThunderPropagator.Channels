using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.ResourceMonitoring.Feeders;

namespace ThunderPropagator.Channels.ResourceMonitoring.Configuration
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