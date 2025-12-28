using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThunderPropagator.Infrastructure.Extensions;

namespace ThunderPropagator.Channels.Games.RockPaperScissors
{
    public static class RockPaperScissorsChannelExtensions
    {
        public static IServiceCollection AddRockPaperScissorsChannel(this IServiceCollection services, Action<RockPaperScissorsChannelConfiguration>? channelConfigurator = null)
        {
            RockPaperScissorsChannelConfiguration rockPaperScissorsChannelConfiguration = new();
            channelConfigurator?.Invoke(rockPaperScissorsChannelConfiguration);

            services.AddSingleton(rockPaperScissorsChannelConfiguration);
            services.AddChannel<RockPaperScissorsChannel>();
            services.TryAddSingleton<RockPaperScissorsComputer>();
            services.AddReceiveEvent<RockPaperScissorsChannel, RockPaperScissorsChannelReceiveEvent>();

            return services;
        }
    }
}