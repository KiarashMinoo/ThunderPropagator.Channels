using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.EntityFrameworkCore.Context;
using ThunderPropagator.Channels.Chat.Extensions;

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore.Extensions
{
    public static class ChatEntityFrameworkCoreExtensions
    {
        /// <summary>
        /// Registers the Chat channel backed by <see cref="EntityFrameworkCoreChatContext"/>.
        /// <paramref name="configureDbContext"/> is where the caller selects and configures a
        /// specific relational provider — e.g. <c>options.UseSqlServer(connectionString)</c>,
        /// <c>options.UseNpgsql(connectionString)</c>, a MySQL provider's <c>UseMySql(...)</c>, or
        /// <c>options.UseSqlite(connectionString)</c> — this package itself only depends on
        /// Microsoft.EntityFrameworkCore.Relational and doesn't reference any single provider package.
        /// No migrations ship with this package, since a migration's generated SQL is tied to whichever
        /// provider produced it; scaffold your own once you've picked a provider, e.g.:
        /// <c>dotnet ef migrations add InitialCreate --context ChatDbContext</c>, run from a project
        /// that references both this package and your chosen provider package.
        /// </summary>
        public static IServiceCollection AddChatChannel(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureDbContext,
            Action<ChatChannelConfiguration>? channelConfigurator = null)
        {
            services.AddDbContext<ChatDbContext>(configureDbContext);

            return services.AddChatChannel<EntityFrameworkCoreChatContext>(channelConfigurator);
        }
    }
}
