using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.BuildingBlocks.Infrastructure.SystemResourceMonitor;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.ResourceMonitoring
{
    public static class ResourceMonitoringChannelExtensions
    {
        public static IServiceCollection AddResourceMonitoringChannel(this IServiceCollection services, Action<ResourceMonitoringChannelConfiguration>? channelConfigurator = null)
        {
            ResourceMonitoringChannelConfiguration resourceMonitoringChannelConfiguration = new();
            channelConfigurator?.Invoke(resourceMonitoringChannelConfiguration);

            services
                .AddSingleton(resourceMonitoringChannelConfiguration)
                .AddSystemResourceMonitor()
                .AddChannel<ResourceMonitoringChannel>()
                .AddChannelFeeder<ResourceMonitoringChannel, ResourceMonitoringChannelFeeder, ResourceMonitoringChannelFeederMessage, ResourceMonitoringChannelFeederConfiguration>(configuration =>
                {
                    configuration.Bind(resourceMonitoringChannelConfiguration.FeederConfiguration);
                });

            return services;
        }
    }
}