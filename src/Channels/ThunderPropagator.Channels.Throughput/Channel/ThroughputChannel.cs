using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Throughput.Configuration;
using ThunderPropagator.Channels.Throughput.Metadata;

namespace ThunderPropagator.Channels.Throughput.Channel;

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