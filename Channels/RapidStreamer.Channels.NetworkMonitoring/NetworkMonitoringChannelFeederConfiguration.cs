using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.NetworkMonitoring
{
    internal
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannelFeederConfiguration : AbstractFeederConfiguration;
}