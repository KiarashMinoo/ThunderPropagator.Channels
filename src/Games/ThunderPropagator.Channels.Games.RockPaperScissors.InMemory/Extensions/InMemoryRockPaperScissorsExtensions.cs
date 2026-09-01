using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Games.RockPaperScissors.Configuration;
using ThunderPropagator.Channels.Games.RockPaperScissors.Extensions;
using ThunderPropagator.Channels.Games.RockPaperScissors.InMemory.Context;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.InMemory.Extensions
{
    public static class InMemoryRockPaperScissorsExtensions
    {
        /// <summary>
        /// Registers the RockPaperScissors channel backed by <see cref="InMemoryRockPaperScissorsContext"/>
        /// — an entirely in-process store with no persistence across restarts and no protection
        /// against being wiped out by process recycling, horizontal scale-out (each instance gets its
        /// own, disconnected copy of the data), or a simple crash. This is for tests and demos only;
        /// do not register it for a real deployment. Mirrors ThunderPropagator.Channels.Chat.InMemory's
        /// own AddChatChannel.
        /// </summary>
        public static IServiceCollection AddRockPaperScissorsChannel(
            this IServiceCollection services,
            Action<RockPaperScissorsChannelConfiguration>? channelConfigurator = null)
        {
            // Singleton: InMemoryRockPaperScissorsContext itself is registered Scoped by
            // AddRockPaperScissorsChannel<T> below (matching every other IRockPaperScissorsContext
            // implementation), but the data it wraps has to outlive any one scope, or every new
            // request would see an empty store.
            services.AddSingleton<InMemoryRockPaperScissorsStore>();

            return services.AddRockPaperScissorsChannel<InMemoryRockPaperScissorsContext>(channelConfigurator);
        }
    }
}
