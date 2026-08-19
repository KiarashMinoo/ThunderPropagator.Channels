using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Chat.EntityFrameworkCore;

namespace ThunderPropagator.UnitTests.Channels.Chat.EntityFrameworkCore
{
    /// <summary>
    /// One shared in-memory SQLite connection for the whole test class (xUnit constructs an
    /// IClassFixture once per class, not per [Fact]). This matters because
    /// BaseChatContext.Migrate()/Seed() only ever run once per process — a static flag, not scoped
    /// per DbContext or connection — so a fresh per-test connection would only get its schema on the
    /// very first test; every later test would hit "no such table". Constructing one
    /// EntityFrameworkCoreChatContext here runs the real Migrate() (against the scaffolded SQLite
    /// migration in this project's own Migrations folder) exactly once, and every test then opens
    /// its own ChatDbContext against the same already-migrated connection.
    /// </summary>
    public sealed class ChatDatabaseFixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public ChatDatabaseFixture()
        {
            _connection.Open();

            using var dbContext = CreateDbContext();
            _ = new EntityFrameworkCoreChatContext(dbContext);
        }

        public ChatDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
            optionsBuilder.UseSqlite(_connection,
                sqlite => sqlite.MigrationsAssembly(typeof(ChatDbContextDesignTimeFactory).Assembly.GetName().Name));
            return new ChatDbContext(optionsBuilder.Options);
        }

        public void Dispose() => _connection.Dispose();
    }
}
