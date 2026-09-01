using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Context;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors.EntityFrameworkCore
{
    /// <summary>
    /// One shared in-memory SQLite connection for the whole test class — mirrors
    /// ThunderPropagator.Channels.Chat.EntityFrameworkCore.UnitTests' own ChatDatabaseFixture; see its
    /// own comment for the full reasoning (BaseRockPaperScissorsContext.InitializeAsync only ever runs
    /// Migrate/Seed once per process, so every test in the class must share one already-migrated
    /// connection rather than each getting a fresh, unmigrated one).
    /// </summary>
    public sealed class RockPaperScissorsDatabaseFixture : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public async Task InitializeAsync()
        {
            _connection.Open();

            using var dbContext = CreateDbContext();
            await new EntityFrameworkCoreRockPaperScissorsContext(dbContext).InitializeAsync();
        }

        public RockPaperScissorsDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<RockPaperScissorsDbContext>();
            optionsBuilder.UseSqlite(_connection,
                sqlite => sqlite.MigrationsAssembly(RockPaperScissorsDbContextTestMigrationsConfiguration.MigrationsAssembly));
            return new RockPaperScissorsDbContext(optionsBuilder.Options);
        }

        public Task DisposeAsync()
        {
            _connection.Dispose();
            return Task.CompletedTask;
        }
    }
}
