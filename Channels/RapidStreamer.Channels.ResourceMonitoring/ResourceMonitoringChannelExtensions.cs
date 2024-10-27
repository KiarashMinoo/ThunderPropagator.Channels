using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.ResourceMonitoring
{
    public static class ResourceMonitoringChannelExtensions
    {
        public static IServiceCollection AddResourceMonitoringChannel(this IServiceCollection services, IConfigurationSection configurationSection)
        {
            services
                .AddChannel<ResourceMonitoringChannel>()
                .AddChannelFeeder<ResourceMonitoringChannel, ResourceMonitoringChannelFeeder, ResourceMonitoringChannelFeederMessage, ResourceMonitoringChannelFeederConfiguration>(configurationSection);

            return services;
        }
    }
}