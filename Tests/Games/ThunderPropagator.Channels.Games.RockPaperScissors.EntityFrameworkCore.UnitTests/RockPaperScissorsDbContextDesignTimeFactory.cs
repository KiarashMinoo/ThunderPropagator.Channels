using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Context;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors.EntityFrameworkCore
{
    /// <summary>
    /// Used only by `dotnet ef migrations add` to scaffold the SQLite migration this test project's
    /// tests run against — mirrors ThunderPropagator.Channels.Chat.EntityFrameworkCore.UnitTests' own
    /// ChatDbContextDesignTimeFactory.
    /// </summary>
    public sealed class RockPaperScissorsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<RockPaperScissorsDbContext>
    {
        public RockPaperScissorsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RockPaperScissorsDbContext>();
            optionsBuilder.UseSqlite(RockPaperScissorsDbContextTestMigrationsConfiguration.DesignTimeConnectionString,
                sqlite => sqlite.MigrationsAssembly(RockPaperScissorsDbContextTestMigrationsConfiguration.MigrationsAssembly));
            return new RockPaperScissorsDbContext(optionsBuilder.Options);
        }
    }
}
