using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Extensions;
using ThunderPropagator.Channels.Chat.MongoDB.Context;

namespace ThunderPropagator.Channels.Chat.MongoDB.Extensions
{
    public static class ChatMongoDbExtensions
    {
        /// <summary>
        /// Registers the Chat channel backed by <see cref="MongoDbChatContext"/>.
        /// <paramref name="configureSettings"/> must set both <see cref="ChatMongoDbSettings.ConnectionString"/>
        /// and <see cref="ChatMongoDbSettings.DatabaseName"/> — validated eagerly here so a
        /// misconfiguration fails at registration time, not on the first request that touches Chat.
        /// </summary>
        public static IServiceCollection AddChatChannel(
            this IServiceCollection services,
            Action<ChatMongoDbSettings> configureSettings,
            Action<ChatChannelConfiguration>? channelConfigurator = null)
        {
            var settings = new ChatMongoDbSettings();
            configureSettings(settings);

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new ArgumentException($"{nameof(ChatMongoDbSettings.ConnectionString)} must be set.", nameof(configureSettings));

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
                throw new ArgumentException($"{nameof(ChatMongoDbSettings.DatabaseName)} must be set.", nameof(configureSettings));

            services
                .AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString))
                .AddScoped(serviceProvider => serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName));

            return services.AddChatChannel<MongoDbChatContext>(channelConfigurator);
        }
    }
}
