using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Chat.EntityFrameworkCore;

namespace ThunderPropagator.UnitTests.Channels.Chat.EntityFrameworkCore
{
    /// <summary>
    /// One shared in-memory SQLite connection for the whole test class (xUnit constructs an
    /// IClassFixture once per class, not per [Fact]). This matters because
    /// BaseChatContext.InitializeAsync() only ever runs Migrate/Seed once per process (#113/#114) —
    /// a static flag, not scoped per DbContext or connection — so a fresh per-test connection would
    /// only get its schema on the very first test; every later test would hit "no such table".
    /// Awaiting InitializeAsync() here (issue #114 moved Migrate/Seed off the constructor and onto
    /// this explicit, cancellable, awaited method) runs the real migration — against the scaffolded
    /// SQLite migration in this project's own Migrations folder — exactly once, and every test then
    /// opens its own ChatDbContext against the same already-migrated connection.
    /// </summary>
    public sealed class ChatDatabaseFixture : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public async Task InitializeAsync()
        {
            _connection.Open();

            using var dbContext = CreateDbContext();
            await new EntityFrameworkCoreChatContext(dbContext).InitializeAsync();
        }

        public ChatDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
            optionsBuilder.UseSqlite(_connection,
                sqlite => sqlite.MigrationsAssembly(typeof(ChatDbContextDesignTimeFactory).Assembly.GetName().Name));
            return new ChatDbContext(optionsBuilder.Options);
        }

        public Task DisposeAsync()
        {
            _connection.Dispose();
            return Task.CompletedTask;
        }
    }
}
