using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.Channels.Games.TicTacToe.EntityFrameworkCore.Context
{
    /// <summary>
    /// The real EF Core <see cref="DbContext"/> for the TicTacToe domain — mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore's own
    /// RockPaperScissorsDbContext. Intentionally provider-agnostic: relies on the caller (see
    /// <see cref="Extensions.TicTacToeEntityFrameworkCoreExtensions"/>) to select and configure a
    /// specific relational provider. No migrations ship with this package for the same reason
    /// ChatDbContext ships none — a migration's generated SQL is tied to whichever provider was
    /// active when it was scaffolded.
    /// </summary>
    public sealed class TicTacToeDbContext(DbContextOptions<TicTacToeDbContext> options) : DbContext(options)
    {
        public DbSet<TicTacToeGameRecord> Games => Set<TicTacToeGameRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicTacToeDbContext).Assembly);
        }
    }
}
