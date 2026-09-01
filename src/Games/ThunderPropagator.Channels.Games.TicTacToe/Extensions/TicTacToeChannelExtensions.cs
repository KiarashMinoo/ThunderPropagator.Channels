using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Games.TicTacToe.Pipelines.AddGame;
using ThunderPropagator.Channels.Games.TicTacToe.Pipelines.GetGames;
using ThunderPropagator.Channels.Games.TicTacToe.Pipelines.Move;
using ThunderPropagator.Channels.Games.TicTacToe.Pipelines.StartGame;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Games.TicTacToe.Channel;
using ThunderPropagator.Channels.Games.TicTacToe.Configuration;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.Channels.Games.TicTacToe.Extensions
{
    public static class TicTacToeChannelExtensions
    {
        // Issue: generic over TContext, mirroring ThunderPropagator.Channels.Games.RockPaperScissors's
        // own AddRockPaperScissorsChannel<TContext> (#288) — a consumer picks a persistence provider
        // (InMemory/EntityFrameworkCore/MongoDB) for TicTacToeGameRecord the same way it already does
        // for RockPaperScissors and Chat.
        public static IServiceCollection AddTicTacToeChannel<TContext>(this IServiceCollection services, Action<TicTacToeChannelConfiguration>? channelConfigurator = null)
            where TContext : BaseTicTacToeContext
        {
            TicTacToeChannelConfiguration ticTacToeChannelConfiguration = new();
            channelConfigurator?.Invoke(ticTacToeChannelConfiguration);

            services
                .AddSingleton(ticTacToeChannelConfiguration)
                .AddChannel<TicTacToeChannel>()
                .AddReceivePipeline<TicTacToeChannel, TicTacToeChannelAddGameReceiverPipeline>()
                .AddReceivePipeline<TicTacToeChannel, TicTacToeChannelGetGamesReceiverPipeline>()
                .AddReceivePipeline<TicTacToeChannel, TicTacToeChannelMoveReceiverPipeline>()
                .AddReceivePipeline<TicTacToeChannel, TicTacToeChannelStartGameReceiverPipeline>()
                .AddScoped<TContext>()
                .AddScoped<ITicTacToeContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>())
                .AddScoped<TicTacToeGameService>()
                // Awaited during host startup, before the host starts accepting traffic — see
                // TicTacToeContextInitializationHostedService.
                .AddHostedService<TicTacToeContextInitializationHostedService<TContext>>();

            return services;
        }
    }
}
