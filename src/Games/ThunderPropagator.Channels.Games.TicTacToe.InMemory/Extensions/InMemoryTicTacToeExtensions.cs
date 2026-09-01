using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Games.TicTacToe.Configuration;
using ThunderPropagator.Channels.Games.TicTacToe.Extensions;
using ThunderPropagator.Channels.Games.TicTacToe.InMemory.Context;

namespace ThunderPropagator.Channels.Games.TicTacToe.InMemory.Extensions
{
    public static class InMemoryTicTacToeExtensions
    {
        /// <summary>
        /// Registers the TicTacToe channel backed by <see cref="InMemoryTicTacToeContext"/> — an
        /// entirely in-process store with no persistence across restarts and no protection against
        /// being wiped out by process recycling, horizontal scale-out (each instance gets its own,
        /// disconnected copy of the data), or a simple crash. This is for tests and demos only; do not
        /// register it for a real deployment. Mirrors
        /// ThunderPropagator.Channels.Games.RockPaperScissors.InMemory's own AddRockPaperScissorsChannel.
        /// </summary>
        public static IServiceCollection AddTicTacToeChannel(
            this IServiceCollection services,
            Action<TicTacToeChannelConfiguration>? channelConfigurator = null)
        {
            // Singleton: InMemoryTicTacToeContext itself is registered Scoped by
            // AddTicTacToeChannel<T> below (matching every other ITicTacToeContext implementation),
            // but the data it wraps has to outlive any one scope, or every new request would see an
            // empty store.
            services.AddSingleton<InMemoryTicTacToeStore>();

            return services.AddTicTacToeChannel<InMemoryTicTacToeContext>(channelConfigurator);
        }
    }
}
