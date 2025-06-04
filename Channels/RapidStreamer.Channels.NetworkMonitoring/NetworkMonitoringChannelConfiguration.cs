using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.NetworkMonitoring
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