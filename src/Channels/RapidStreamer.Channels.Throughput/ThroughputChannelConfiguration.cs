using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Throughput
{
    public
#if !DEBUG
        sealed
#endif
        class ThroughputChannelConfiguration : AbstractChannelConfiguration
    {
        public ThroughputChannelFeederConfiguration FeederConfiguration { get; set; } = new();

        public ThroughputChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}