using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Throughput.Channel;
using ThunderPropagator.Channels.Throughput.Configuration;
using ThunderPropagator.Channels.Throughput.Feeders;
using ThunderPropagator.Channels.Throughput.Messages;

namespace ThunderPropagator.Channels.Throughput.Extensions
{
    public static class ThroughputChannelExtensions
    {
        public static IServiceCollection AddThroughputChannel(this IServiceCollection services, Action<ThroughputChannelConfiguration>? channelConfigurator = null)
        {
            ThroughputChannelConfiguration throughputChannelConfiguration = new();
            channelConfigurator?.Invoke(throughputChannelConfiguration);

            services
                .AddSingleton(throughputChannelConfiguration)
                .AddChannel<ThroughputChannel>()
                .AddChannelFeeder<ThroughputChannel, ThroughputChannelFeeder, ThroughputChannelFeederMessage, ThroughputChannelFeederConfiguration>(configuration => configuration.Bind(throughputChannelConfiguration.FeederConfiguration));

            return services;
        }
    }
}