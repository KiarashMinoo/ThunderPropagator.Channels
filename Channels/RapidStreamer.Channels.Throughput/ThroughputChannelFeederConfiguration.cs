using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Throughput
{
    public
#if !DEBUG
        sealed
#endif
        class ThroughputChannelFeederConfiguration : AbstractFeederConfiguration
    {
        public ThroughputChannelFeederConfiguration()
        {
            IsEnabled = true;
        }

        internal void Bind(ThroughputChannelFeederConfiguration throughputChannelFeederConfiguration) => base.Bind(throughputChannelFeederConfiguration);
    }
}