using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Games.RockPaperScissors
{
    public static class RockPaperScissorsChannelExtensions
    {
        public static IServiceCollection AddRockPaperScissorsChannel(this IServiceCollection services)
        {
            services.AddChannel<RockPaperScissorsChannel>();
            services.TryAddSingleton<RockPaperScissorsComputer>();
            services.AddReceiveEvent<RockPaperScissorsChannel, RockPaperScissorsChannelReceiveEvent>();

            return services;
        }
    }
}