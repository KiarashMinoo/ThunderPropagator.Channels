using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.NetworkMonitoring
{
    public
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannelFeederConfiguration : AbstractFeederConfiguration
    {
        public NetworkMonitoringChannelFeederConfiguration()
        {
            IsEnabled = true;
        }

        internal void Bind(NetworkMonitoringChannelFeederConfiguration networkMonitoringChannelFeederConfiguration) => base.Bind(networkMonitoringChannelFeederConfiguration);
    }
}