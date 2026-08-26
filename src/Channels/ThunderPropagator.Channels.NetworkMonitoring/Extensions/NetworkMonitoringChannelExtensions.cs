using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.NetworkMonitoring.Channel;
using ThunderPropagator.Channels.NetworkMonitoring.Configuration;
using ThunderPropagator.Channels.NetworkMonitoring.Feeders;
using ThunderPropagator.Channels.NetworkMonitoring.Messages;

namespace ThunderPropagator.Channels.NetworkMonitoring.Extensions
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