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
    /// tests and are never referenced by the shipped package.
    /// </summary>
    public sealed class ChatDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ChatDbContext>
    {
        public ChatDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
            // Keeps the scaffolded migrations in this test project's own assembly instead of the
            // shipped ChatDbContext's assembly (EF's default) — see this class's doc comment.
            optionsBuilder.UseSqlite("Data Source=design-time.db",
                sqlite => sqlite.MigrationsAssembly(typeof(ChatDbContextDesignTimeFactory).Assembly.GetName().Name));
            return new ChatDbContext(optionsBuilder.Options);
        }
    }
}
