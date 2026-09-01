using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Context
{
    /// <summary>
    /// Implements the RockPaperScissors persistence contract on top of <see cref="RockPaperScissorsDbContext"/>
    /// — mirrors ThunderPropagator.Channels.Chat.EntityFrameworkCore's own EntityFrameworkCoreChatContext.
    /// </summary>
    public sealed class EntityFrameworkCoreRockPaperScissorsContext(RockPaperScissorsDbContext dbContext) : BaseRockPaperScissorsContext
    {
        protected override Task MigrateAsync(CancellationToken cancellationToken) => dbContext.Database.MigrateAsync(cancellationToken);

        // No default seed data.
        protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override async Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
            => await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);

        public override async Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
            => await dbContext.Set<TEntity>().ToListAsync(cancellationToken);

        public override async Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            dbContext.Set<TEntity>().Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        /// <summary>
        /// ConnectionId is <see cref="RockPaperScissorsMatchReservation"/>'s own primary key (see
        /// <see cref="Configurations.RockPaperScissorsMatchReservationConfiguration"/>), so a second
        /// insert for an already-reserved connectionId fails the unique constraint the primary key
        /// itself enforces — caught here and turned into a false return rather than a generic
        /// CreateAsync a caller would need to wrap in its own try/catch.
        /// </summary>
        public override async Task<bool> TryReserveConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            dbContext.Add(RockPaperScissorsMatchReservation.Create(connectionId));

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                dbContext.ChangeTracker.Clear();
                return false;
            }
        }
    }
}
