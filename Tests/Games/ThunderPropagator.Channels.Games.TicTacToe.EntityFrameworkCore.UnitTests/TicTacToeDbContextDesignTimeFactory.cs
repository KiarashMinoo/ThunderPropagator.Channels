using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ThunderPropagator.Channels.Games.TicTacToe.EntityFrameworkCore.Context;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.EntityFrameworkCore
{
    /// <summary>
    /// Used only by `dotnet ef migrations add` to scaffold the SQLite migration this test project's
    /// tests run against — mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.UnitTests' own
    /// RockPaperScissorsDbContextDesignTimeFactory.
    /// </summary>
    public sealed class TicTacToeDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TicTacToeDbContext>
    {
        public TicTacToeDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TicTacToeDbContext>();
            optionsBuilder.UseSqlite(TicTacToeDbContextTestMigrationsConfiguration.DesignTimeConnectionString,
                sqlite => sqlite.MigrationsAssembly(TicTacToeDbContextTestMigrationsConfiguration.MigrationsAssembly));
            return new TicTacToeDbContext(optionsBuilder.Options);
        }
    }
}
