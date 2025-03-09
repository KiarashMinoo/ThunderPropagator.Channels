using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.BuildingBlocks.Infrastructure.SystemResourceMonitor;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.ResourceMonitoring
{
    public static class ResourceMonitoringChannelExtensions
    {
        public static IServiceCollection AddResourceMonitoringChannel(this IServiceCollection services, IConfigurationSection configurationSection)
        {
            services
                .AddSystemResourceMonitor()
                .AddChannel<ResourceMonitoringChannel>()
                .AddChannelFeeder<ResourceMonitoringChannel, ResourceMonitoringChannelFeeder, ResourceMonitoringChannelFeederMessage, ResourceMonitoringChannelFeederConfiguration>(configurationSection);

            return services;
        }
    }
}