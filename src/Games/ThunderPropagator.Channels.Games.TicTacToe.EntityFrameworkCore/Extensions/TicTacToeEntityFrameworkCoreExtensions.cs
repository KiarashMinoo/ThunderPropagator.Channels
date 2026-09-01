using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Games.TicTacToe.Configuration;
using ThunderPropagator.Channels.Games.TicTacToe.EntityFrameworkCore.Context;
using ThunderPropagator.Channels.Games.TicTacToe.Extensions;

namespace ThunderPropagator.Channels.Games.TicTacToe.EntityFrameworkCore.Extensions
{
    public static class TicTacToeEntityFrameworkCoreExtensions
    {
        /// <summary>
        /// Registers the TicTacToe channel backed by <see cref="EntityFrameworkCoreTicTacToeContext"/>.
        /// <paramref name="configureDbContext"/> is where the caller selects and configures a specific
        /// relational provider — mirrors
        /// ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore's own
        /// AddRockPaperScissorsChannel. No migrations ship with this package; scaffold your own once
        /// you've picked a provider, e.g. <c>dotnet ef migrations add InitialCreate --context TicTacToeDbContext</c>.
        /// </summary>
        public static IServiceCollection AddTicTacToeChannel(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureDbContext,
            Action<TicTacToeChannelConfiguration>? channelConfigurator = null)
        {
            services.AddDbContext<TicTacToeDbContext>(configureDbContext);

            return services.AddTicTacToeChannel<EntityFrameworkCoreTicTacToeContext>(channelConfigurator);
        }
    }
}
