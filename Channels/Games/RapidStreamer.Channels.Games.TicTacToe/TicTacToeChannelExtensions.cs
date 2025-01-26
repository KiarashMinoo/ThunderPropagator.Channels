using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Infrastructure.Extensions;

namespace RapidStreamer.Channels.Games.TicTacToe
{
    public static class TicTacToeChannelExtensions
    {
        public static IServiceCollection AddTicTacToeChannel(this IServiceCollection services)
        {
            services.AddChannel<TicTacToeChannel>();

            return services;
        }
    }
}