using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ThunderPropagator.Channels.Games.TicTacToe.Configuration;
using ThunderPropagator.Channels.Games.TicTacToe.Extensions;
using ThunderPropagator.Channels.Games.TicTacToe.MongoDB.Context;

namespace ThunderPropagator.Channels.Games.TicTacToe.MongoDB.Extensions
{
    public static class TicTacToeMongoDbExtensions
    {
        /// <summary>
        /// Registers the TicTacToe channel backed by <see cref="MongoDbTicTacToeContext"/>.
        /// <paramref name="configureSettings"/> must set both <see cref="TicTacToeMongoDbSettings.ConnectionString"/>
        /// and <see cref="TicTacToeMongoDbSettings.DatabaseName"/> — validated eagerly here so a
        /// misconfiguration fails at registration time, not on the first request that touches
        /// TicTacToe. Mirrors ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB's own
        /// AddRockPaperScissorsChannel.
        /// </summary>
        public static IServiceCollection AddTicTacToeChannel(
            this IServiceCollection services,
            Action<TicTacToeMongoDbSettings> configureSettings,
            Action<TicTacToeChannelConfiguration>? channelConfigurator = null)
        {
            var settings = new TicTacToeMongoDbSettings();
            configureSettings(settings);

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new ArgumentException($"{nameof(TicTacToeMongoDbSettings.ConnectionString)} must be set.", nameof(configureSettings));

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
                throw new ArgumentException($"{nameof(TicTacToeMongoDbSettings.DatabaseName)} must be set.", nameof(configureSettings));

            services
                .AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString))
                .AddScoped(serviceProvider => serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName));

            return services.AddTicTacToeChannel<MongoDbTicTacToeContext>(channelConfigurator);
        }
    }
}
