using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.NetworkMonitoring
{
    public
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannelConfiguration : AbstractChannelConfiguration
    {
        public NetworkMonitoringChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}