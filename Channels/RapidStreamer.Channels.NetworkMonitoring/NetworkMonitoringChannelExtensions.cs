using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.NetworkMonitoring
{
    public static class NetworkMonitoringChannelExtensions
    {
        public static IServiceCollection AddNetworkMonitoringChannel(this IServiceCollection services)
        {
            services.AddChannel<NetworkMonitoringChannel>()
                .AddChannelFeeder<NetworkMonitoringChannel, NetworkMonitoringChannelFeeder, NetworkMonitoringChannelFeederMessage, NetworkMonitoringChannelFeederConfiguration>();

            return services;
        }
    }
}