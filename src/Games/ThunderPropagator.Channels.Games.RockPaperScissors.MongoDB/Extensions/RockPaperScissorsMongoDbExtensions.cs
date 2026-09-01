using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ThunderPropagator.Channels.Games.RockPaperScissors.Configuration;
using ThunderPropagator.Channels.Games.RockPaperScissors.Extensions;
using ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Context;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Extensions
{
    public static class RockPaperScissorsMongoDbExtensions
    {
        /// <summary>
        /// Registers the RockPaperScissors channel backed by <see cref="MongoDbRockPaperScissorsContext"/>.
        /// <paramref name="configureSettings"/> must set both <see cref="RockPaperScissorsMongoDbSettings.ConnectionString"/>
        /// and <see cref="RockPaperScissorsMongoDbSettings.DatabaseName"/> — validated eagerly here so
        /// a misconfiguration fails at registration time, not on the first request that touches
        /// RockPaperScissors. Mirrors ThunderPropagator.Channels.Chat.MongoDB's own AddChatChannel.
        /// </summary>
        public static IServiceCollection AddRockPaperScissorsChannel(
            this IServiceCollection services,
            Action<RockPaperScissorsMongoDbSettings> configureSettings,
            Action<RockPaperScissorsChannelConfiguration>? channelConfigurator = null)
        {
            var settings = new RockPaperScissorsMongoDbSettings();
            configureSettings(settings);

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new ArgumentException($"{nameof(RockPaperScissorsMongoDbSettings.ConnectionString)} must be set.", nameof(configureSettings));

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
                throw new ArgumentException($"{nameof(RockPaperScissorsMongoDbSettings.DatabaseName)} must be set.", nameof(configureSettings));

            services
                .AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString))
                .AddScoped(serviceProvider => serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName));

            return services.AddRockPaperScissorsChannel<MongoDbRockPaperScissorsContext>(channelConfigurator);
        }
    }
}
