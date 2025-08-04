using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Throughput
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