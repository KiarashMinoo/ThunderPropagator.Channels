using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.NetworkMonitoring
{
    public
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannel : AbstractChannel<NetworkMonitoringChannelMetadata, NetworkMonitoringChannelConfiguration>
    {
        public NetworkMonitoringChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}