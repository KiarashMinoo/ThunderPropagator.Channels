using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Games.TicTacToe.Pipelines.AddGame;
using ThunderPropagator.Channels.Games.TicTacToe.Pipelines.GetGames;
using ThunderPropagator.Channels.Games.TicTacToe.Pipelines.Move;
using ThunderPropagator.Channels.Games.TicTacToe.Pipelines.StartGame;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Games.TicTacToe.Channel;
using ThunderPropagator.Channels.Games.TicTacToe.Configuration;

namespace ThunderPropagator.Channels.Games.TicTacToe.Extensions
{
    public static class TicTacToeChannelExtensions
    {
        public static IServiceCollection AddTicTacToeChannel(this IServiceCollection services, Action<TicTacToeChannelConfiguration>? channelConfigurator = null)
        {
            TicTacToeChannelConfiguration ticTacToeChannelConfiguration = new();
            channelConfigurator?.Invoke(ticTacToeChannelConfiguration);

            services
                .AddSingleton(ticTacToeChannelConfiguration)
                .AddChannel<TicTacToeChannel>()
                .AddReceivePipeline<TicTacToeChannel, TicTacToeChannelAddGameReceiverPipeline>()
                .AddReceivePipeline<TicTacToeChannel, TicTacToeChannelGetGamesReceiverPipeline>()
                .AddReceivePipeline<TicTacToeChannel, TicTacToeChannelMoveReceiverPipeline>()
                .AddReceivePipeline<TicTacToeChannel, TicTacToeChannelStartGameReceiverPipeline>();

            return services;
        }
    }
}