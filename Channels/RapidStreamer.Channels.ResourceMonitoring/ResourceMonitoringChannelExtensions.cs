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
                    configuration.IsEnabled = resourceMonitoringChannelConfiguration.FeederConfiguration.IsEnabled;
                    configuration.Id = resourceMonitoringChannelConfiguration.FeederConfiguration.Id;
                    configuration.SerializerType = resourceMonitoringChannelConfiguration.FeederConfiguration.SerializerType;
                    configuration.EnrichmentScript = resourceMonitoringChannelConfiguration.FeederConfiguration.EnrichmentScript;
                    configuration.MetadataReferences = resourceMonitoringChannelConfiguration.FeederConfiguration.MetadataReferences;
                    configuration.UtilizationWindow = resourceMonitoringChannelConfiguration.FeederConfiguration.UtilizationWindow;
                    configuration.MemoryUsedPercentageThreshold = resourceMonitoringChannelConfiguration.FeederConfiguration.MemoryUsedPercentageThreshold;
                    configuration.StorageUsedPercentageThreshold = resourceMonitoringChannelConfiguration.FeederConfiguration.StorageUsedPercentageThreshold;
                });

            return services;
        }
    }
}