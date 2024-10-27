using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Throughput
{
    public static class ThroughputChannelExtensions
    {
        public static IServiceCollection AddThroughputChannel(this IServiceCollection services)
        {
            services.AddChannel<ThroughputChannel>().AddChannelFeeder<ThroughputChannel, ThroughputChannelFeeder, ThroughputChannelFeederMessage, ThroughputChannelFeederConfiguration>();

            return services;
        }
    }
}