using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.NetworkMonitoring.Configuration;
using ThunderPropagator.Channels.NetworkMonitoring.Metadata;

namespace ThunderPropagator.Channels.NetworkMonitoring.Channel
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