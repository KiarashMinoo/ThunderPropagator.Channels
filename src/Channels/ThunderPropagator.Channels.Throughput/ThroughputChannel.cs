using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Throughput;

public
#if !DEBUG
    sealed
#endif
    class ThroughputChannel : AbstractChannel<ThroughputChannelMetadata, ThroughputChannelConfiguration>
{
    public ThroughputChannel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}