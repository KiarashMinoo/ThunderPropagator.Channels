using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.Throughput
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