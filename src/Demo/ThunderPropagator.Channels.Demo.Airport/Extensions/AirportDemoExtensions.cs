using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Demo.Airport.Channel;
using ThunderPropagator.Channels.Demo.Airport.Configuration;
using ThunderPropagator.Channels.Demo.Airport.Feeders;
using ThunderPropagator.Channels.Demo.Airport.Messages;

namespace ThunderPropagator.Channels.Demo.Airport.Extensions
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