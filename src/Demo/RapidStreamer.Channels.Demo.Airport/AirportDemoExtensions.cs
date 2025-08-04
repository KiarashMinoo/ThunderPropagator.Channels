using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Demo.Airport
{
    public static class AirportDemoExtensions
    {
        public static IServiceCollection AddAirportDemoChannel(this IServiceCollection services, Action<AirportDemoChannelConfiguration>? channelConfigurator = null)
        {
            AirportDemoChannelConfiguration airportDemoChannelConfiguration = new();
            channelConfigurator?.Invoke(airportDemoChannelConfiguration);

            services
                .AddSingleton(airportDemoChannelConfiguration)
                .AddChannel<AirportDemoChannel>()
                .AddChannelFeeder<AirportDemoChannel, AirportDemoChannelFeeder, AirportDemoChannelFeederMessage, AirportDemoChannelFeederConfiguration>(configuration => configuration.Bind(airportDemoChannelConfiguration.FeederConfiguration));

            return services;
        }
    }
}