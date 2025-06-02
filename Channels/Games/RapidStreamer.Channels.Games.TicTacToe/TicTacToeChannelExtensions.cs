using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Channels.Games.TicTacToe.Pipelines.AddGame;
using RapidStreamer.Channels.Games.TicTacToe.Pipelines.GetGames;
using RapidStreamer.Channels.Games.TicTacToe.Pipelines.Move;
using RapidStreamer.Channels.Games.TicTacToe.Pipelines.StartGame;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Games.TicTacToe
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