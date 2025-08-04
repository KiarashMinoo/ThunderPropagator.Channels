using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.BuildingBlocks.Infrastructure.SystemResourceMonitor;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.ResourceMonitoring
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