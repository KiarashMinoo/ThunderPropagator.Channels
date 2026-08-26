using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.ResourceMonitoring.Configuration;
using ThunderPropagator.Channels.ResourceMonitoring.Metadata;

namespace ThunderPropagator.Channels.ResourceMonitoring.Channel;

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