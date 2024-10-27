using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Throughput;

public
#if !DEBUG
        sealed
#endif
    class ThroughputChannel : AbstractChannel<ThroughputChannelMetadata>
{
    public ThroughputChannel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}