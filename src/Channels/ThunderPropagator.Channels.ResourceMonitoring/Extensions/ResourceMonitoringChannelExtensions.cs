using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.BuildingBlocks.Infrastructure.SystemResourceMonitor;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.ResourceMonitoring.Channel;
using ThunderPropagator.Channels.ResourceMonitoring.Configuration;
using ThunderPropagator.Channels.ResourceMonitoring.Feeders;
using ThunderPropagator.Channels.ResourceMonitoring.Messages;

namespace ThunderPropagator.Channels.ResourceMonitoring.Extensions
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