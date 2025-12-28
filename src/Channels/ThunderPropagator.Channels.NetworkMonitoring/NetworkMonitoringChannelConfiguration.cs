using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.NetworkMonitoring
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