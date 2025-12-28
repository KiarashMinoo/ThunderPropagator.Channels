using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.NetworkMonitoring
{
    public static class NetworkMonitoringChannelExtensions
    {
        public static IServiceCollection AddNetworkMonitoringChannel(this IServiceCollection services, Action<NetworkMonitoringChannelConfiguration>? channelConfigurator = null)
        {
            NetworkMonitoringChannelConfiguration networkMonitoringChannelConfiguration = new();
            channelConfigurator?.Invoke(networkMonitoringChannelConfiguration);

            services
                .AddSingleton(networkMonitoringChannelConfiguration)
                .AddChannel<NetworkMonitoringChannel>()
                .AddChannelFeeder<NetworkMonitoringChannel, NetworkMonitoringChannelFeeder, NetworkMonitoringChannelFeederMessage, NetworkMonitoringChannelFeederConfiguration>(configuration =>
                    configuration.Bind(networkMonitoringChannelConfiguration.FeederConfiguration));

            return services;
        }
    }
}