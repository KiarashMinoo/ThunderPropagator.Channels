using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Demo.Airport
{
    public static class AirportDemoExtensions
    {
        public static IServiceCollection AddAirportDemoChannel(this IServiceCollection services)
        {
            services.AddChannel<AirportDemoChannel>()
                .AddChannelFeeder<AirportDemoChannel, AirportDemoChannelFeeder, AirportDemoChannelFeederMessage, AirportDemoChannelFeederConfiguration>();

            return services;
        }
    }
}