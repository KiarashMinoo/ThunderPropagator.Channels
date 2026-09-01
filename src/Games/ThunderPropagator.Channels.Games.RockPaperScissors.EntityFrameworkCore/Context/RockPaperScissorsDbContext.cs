using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;
using ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Extensions;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Context
{
    /// <summary>
    /// The real EF Core <see cref="DbContext"/> for the RockPaperScissors domain — mirrors
    /// ThunderPropagator.Channels.Chat.EntityFrameworkCore's own ChatDbContext. Intentionally
    /// provider-agnostic: relies on the caller (see <see cref="RockPaperScissorsEntityFrameworkCoreExtensions"/>)
    /// to select and configure a specific relational provider. No migrations ship with this package
    /// for the same reason ChatDbContext ships none — a migration's generated SQL is tied to whichever
    /// provider was active when it was scaffolded.
    /// </summary>
    public sealed class RockPaperScissorsDbContext(DbContextOptions<RockPaperScissorsDbContext> options) : DbContext(options)
    {
        public DbSet<RockPaperScissorsMatchReservation> MatchReservations => Set<RockPaperScissorsMatchReservation>();
        public DbSet<RockPaperScissorsGameSessionRecord> GameSessionRecords => Set<RockPaperScissorsGameSessionRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(RockPaperScissorsDbContext).Assembly);
        }
    }
}
