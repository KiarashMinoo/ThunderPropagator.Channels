using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Throughput.Feeders;

namespace ThunderPropagator.Channels.Throughput.Configuration
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