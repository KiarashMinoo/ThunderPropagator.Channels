using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Games.TicTacToe.EntityFrameworkCore.Context;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.EntityFrameworkCore
{
    /// <summary>
    /// One shared in-memory SQLite connection for the whole test class — mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.UnitTests' own
    /// RockPaperScissorsDatabaseFixture; see its own comment for the full reasoning.
    /// </summary>
    public sealed class TicTacToeDatabaseFixture : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public async Task InitializeAsync()
        {
            _connection.Open();

            using var dbContext = CreateDbContext();
            await new EntityFrameworkCoreTicTacToeContext(dbContext).InitializeAsync();
        }

        public TicTacToeDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TicTacToeDbContext>();
            optionsBuilder.UseSqlite(_connection,
                sqlite => sqlite.MigrationsAssembly(TicTacToeDbContextTestMigrationsConfiguration.MigrationsAssembly));
            return new TicTacToeDbContext(optionsBuilder.Options);
        }

        public Task DisposeAsync()
        {
            _connection.Dispose();
            return Task.CompletedTask;
        }
    }
}
