using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Games.RockPaperScissors.Configuration;
using ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Context;
using ThunderPropagator.Channels.Games.RockPaperScissors.Extensions;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Extensions
{
    public static class RockPaperScissorsEntityFrameworkCoreExtensions
    {
        /// <summary>
        /// Registers the RockPaperScissors channel backed by <see cref="EntityFrameworkCoreRockPaperScissorsContext"/>.
        /// <paramref name="configureDbContext"/> is where the caller selects and configures a specific
        /// relational provider — mirrors ThunderPropagator.Channels.Chat.EntityFrameworkCore's own
        /// AddChatChannel. No migrations ship with this package; scaffold your own once you've picked
        /// a provider, e.g. <c>dotnet ef migrations add InitialCreate --context RockPaperScissorsDbContext</c>.
        /// </summary>
        public static IServiceCollection AddRockPaperScissorsChannel(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureDbContext,
            Action<RockPaperScissorsChannelConfiguration>? channelConfigurator = null)
        {
            services.AddDbContext<RockPaperScissorsDbContext>(configureDbContext);

            return services.AddRockPaperScissorsChannel<EntityFrameworkCoreRockPaperScissorsContext>(channelConfigurator);
        }
    }
}
