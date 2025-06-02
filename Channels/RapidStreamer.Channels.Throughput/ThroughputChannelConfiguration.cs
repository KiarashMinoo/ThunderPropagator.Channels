using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Throughput
{
    public
#if !DEBUG
        sealed
#endif
        class ThroughputChannelConfiguration : AbstractChannelConfiguration
    {
        public ThroughputChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}