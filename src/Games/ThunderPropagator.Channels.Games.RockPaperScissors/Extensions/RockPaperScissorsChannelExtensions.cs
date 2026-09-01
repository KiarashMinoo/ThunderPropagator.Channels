using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Games.RockPaperScissors.Channel;
using ThunderPropagator.Channels.Games.RockPaperScissors.Configuration;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.Extensions
{
    public static class RockPaperScissorsChannelExtensions
    {
        // Issue #288: generic over TContext, mirroring ThunderPropagator.Channels.Chat's
        // AddChatChannel<TChatContext> (#110/#111/#112) — a consumer picks a persistence provider
        // (InMemory/EntityFrameworkCore/MongoDB) for RockPaperScissorsMatchReservation/
        // RockPaperScissorsGameSessionRecord the same way it already does for Chat.
        public static IServiceCollection AddRockPaperScissorsChannel<TContext>(this IServiceCollection services, Action<RockPaperScissorsChannelConfiguration>? channelConfigurator = null)
            where TContext : BaseRockPaperScissorsContext
        {
            RockPaperScissorsChannelConfiguration rockPaperScissorsChannelConfiguration = new();
            channelConfigurator?.Invoke(rockPaperScissorsChannelConfiguration);

            services.AddSingleton(rockPaperScissorsChannelConfiguration);
            services.AddChannel<RockPaperScissorsChannel>();
            services.TryAddSingleton<RockPaperScissorsComputer>();
            services.AddReceiveEvent<RockPaperScissorsChannel, RockPaperScissorsChannelReceiveEvent>();

            services
                .AddScoped<TContext>()
                .AddScoped<IRockPaperScissorsContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>())
                .AddScoped<RockPaperScissorsMatchmakingService>()
                // Issue #114-equivalent: awaited during host startup, before the host starts accepting
                // traffic — see RockPaperScissorsContextInitializationHostedService.
                .AddHostedService<RockPaperScissorsContextInitializationHostedService<TContext>>();

            return services;
        }
    }
}
