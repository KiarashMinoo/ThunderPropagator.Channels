using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Clock
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
                .AddChannelFeeder<ClockChannel, NowClockFeeder, ClockChannelFeederMessage, NowClockFeederConfiguration>()
                .AddChannelFeeder<ClockChannel, UtcNowClockFeeder, ClockChannelFeederMessage, UtcNowClockFeederConfiguration>();

            return services;
        }
    }
}