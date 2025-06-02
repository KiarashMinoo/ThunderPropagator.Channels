using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Throughput;

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