using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.ResourceMonitoring;

public
#if !DEBUG
        sealed
#endif
    class ResourceMonitoringChannel : AbstractChannel<ResourceMonitoringChannelMetadata>
{
    public ResourceMonitoringChannel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}