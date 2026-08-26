using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.Throughput.Feeders
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