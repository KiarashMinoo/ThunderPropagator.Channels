using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.Clock
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