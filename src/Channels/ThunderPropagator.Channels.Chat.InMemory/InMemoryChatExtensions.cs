using Microsoft.Extensions.DependencyInjection;

namespace ThunderPropagator.Channels.Chat.InMemory
{
    public static class InMemoryChatExtensions
    {
        /// <summary>
        /// Registers the Chat channel backed by <see cref="InMemoryChatContext"/> — an entirely
        /// in-process store with no persistence across restarts and no protection against being
        /// wiped out by process recycling, horizontal scale-out (each instance gets its own,
        /// disconnected copy of the data), or a simple crash. This is for tests and demos only; do
        /// not register it for a real deployment. Use the EF Core (#110) or MongoDB (#111) provider
        /// for anything that needs to survive a restart or run behind more than one instance.
        /// </summary>
        public static IServiceCollection AddChatChannel(
            this IServiceCollection services,
            Action<ChatChannelConfiguration>? channelConfigurator = null)
        {
            // Singleton: InMemoryChatContext itself is registered Scoped by AddChatChannel<T> below
            // (matching every other IChatContext implementation), but the data it wraps has to
            // outlive any one scope, or every new request would see an empty store.
            services.AddSingleton<InMemoryChatStore>();

            return services.AddChatChannel<InMemoryChatContext>(channelConfigurator);
        }
    }
}
