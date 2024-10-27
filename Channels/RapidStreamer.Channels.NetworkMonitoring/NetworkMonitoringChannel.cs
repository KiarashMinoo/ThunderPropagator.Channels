using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.NetworkMonitoring
{
    public
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannel : AbstractChannel<NetworkMonitoringChannelMetadata>
    {
        public NetworkMonitoringChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}