using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ThunderPropagator.Channels.Chat.EntityFrameworkCore;

namespace ThunderPropagator.UnitTests.Channels.Chat.EntityFrameworkCore
{
    /// <summary>
    /// Used only by `dotnet ef migrations add` to scaffold the SQLite migration this test project's
    /// integration tests run against. ChatDbContext itself ships with no migrations (see its own doc
    /// comment) since a migration's SQL is tied to whichever provider produced it — this factory, and
    /// the Migrations folder it generates, exist solely for this test project's own SQLite-backed
    /// tests and are never referenced by the shipped package. See
    /// ChatDbContextTestMigrationsConfiguration for where the connection string/migrations assembly
    /// below actually come from.
    /// </summary>
    public sealed class ChatDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ChatDbContext>
    {
        public ChatDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
            optionsBuilder.UseSqlite(ChatDbContextTestMigrationsConfiguration.DesignTimeConnectionString,
                sqlite => sqlite.MigrationsAssembly(ChatDbContextTestMigrationsConfiguration.MigrationsAssembly));
            return new ChatDbContext(optionsBuilder.Options);
        }
    }
}
