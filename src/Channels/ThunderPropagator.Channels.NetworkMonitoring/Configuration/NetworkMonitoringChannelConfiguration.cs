using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.NetworkMonitoring.Feeders;

namespace ThunderPropagator.Channels.NetworkMonitoring.Configuration
{
    public
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannelConfiguration : AbstractChannelConfiguration
    {
        public NetworkMonitoringChannelFeederConfiguration FeederConfiguration { get; set; } = new();
        
        public NetworkMonitoringChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}