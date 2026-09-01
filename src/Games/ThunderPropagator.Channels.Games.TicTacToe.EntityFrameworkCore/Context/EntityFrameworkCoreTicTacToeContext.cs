using Microsoft.EntityFrameworkCore;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.Channels.Games.TicTacToe.EntityFrameworkCore.Context
{
    /// <summary>
    /// Implements the TicTacToe persistence contract on top of <see cref="TicTacToeDbContext"/> —
    /// mirrors ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore's own
    /// EntityFrameworkCoreRockPaperScissorsContext.
    /// </summary>
    public sealed class EntityFrameworkCoreTicTacToeContext(TicTacToeDbContext dbContext) : BaseTicTacToeContext
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

        // Same reasoning as EntityFrameworkCoreChatContext's own UpdateAsync: every caller here fetches
        // the entity through this same context via GetAsync and then mutates it in place
        // (TicTacToeGameRecord.Start/ApplyMove) before calling this, so it's already tracked — only
        // attach when it truly isn't (e.g. a caller that built one without a prior fetch).
        public override async Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            if (dbContext.Entry(entity).State == EntityState.Detached)
                dbContext.Set<TEntity>().Update(entity);

            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public override async Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            var entity = await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);
            if (entity is null)
                return false;

            dbContext.Set<TEntity>().Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
