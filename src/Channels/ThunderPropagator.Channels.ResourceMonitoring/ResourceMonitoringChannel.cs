using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.ResourceMonitoring;

public
#if !DEBUG
    sealed
#endif
    class ResourceMonitoringChannel : AbstractChannel<ResourceMonitoringChannelMetadata, ResourceMonitoringChannelConfiguration>
{
    public ResourceMonitoringChannel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}