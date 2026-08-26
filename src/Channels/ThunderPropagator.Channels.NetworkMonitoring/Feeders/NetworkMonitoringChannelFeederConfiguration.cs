using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.NetworkMonitoring.Feeders
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