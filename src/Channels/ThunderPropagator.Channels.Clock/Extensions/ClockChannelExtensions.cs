using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Clock.Channel;
using ThunderPropagator.Channels.Clock.Configuration;
using ThunderPropagator.Channels.Clock.Feeders;
using ThunderPropagator.Channels.Clock.Messages;

namespace ThunderPropagator.Channels.Clock.Extensions
{
    public static class ClockChannelExtensions
    {
        public static IServiceCollection AddClockChannel(this IServiceCollection services, Action<ClockChannelConfiguration>? channelConfigurator = null)
        {
            ClockChannelConfiguration clockChannelConfiguration = new();
            channelConfigurator?.Invoke(clockChannelConfiguration);

            services
                .AddSingleton(clockChannelConfiguration)
                .AddChannel<ClockChannel>()
                .AddChannelFeeder<ClockChannel, NowClockFeeder, ClockChannelFeederMessage, NowClockFeederConfiguration>(configuration => configuration.Bind(clockChannelConfiguration.NowClockFeederConfiguration))
                .AddChannelFeeder<ClockChannel, UtcNowClockFeeder, ClockChannelFeederMessage, UtcNowClockFeederConfiguration>(configuration => configuration.Bind(clockChannelConfiguration.UtcNowClockFeederConfiguration));

            return services;
        }
    }
}