using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Throughput
{
    internal
#if !DEBUG
        sealed
#endif
        class ThroughputChannelFeederConfiguration : AbstractFeederConfiguration;
}