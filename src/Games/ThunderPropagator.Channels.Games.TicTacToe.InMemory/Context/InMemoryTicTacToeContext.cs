using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.Channels.Games.TicTacToe.InMemory.Context
{
    /// <summary>
    /// Implements the TicTacToe persistence contract over a plain in-process
    /// <see cref="InMemoryTicTacToeStore"/> — no database, no network, deterministic. Intended for
    /// tests and demos only; mirrors ThunderPropagator.Channels.Games.RockPaperScissors.InMemory's own
    /// InMemoryRockPaperScissorsContext. UpdateAsync just re-stores the (already-mutated, since
    /// TicTacToeGameRecord's Start/ApplyMove mutate in place) same entity at its key — there is only
    /// one entity type here and no concurrent-caller isolation this provider promises, matching the
    /// "for tests and demos only" contract every InMemory provider in this repo already carries.
    /// </summary>
    public sealed class InMemoryTicTacToeContext(InMemoryTicTacToeStore store) : BaseTicTacToeContext
    {
        protected override Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = InMemoryTicTacToeStore.ToKey(id);
            return Task.FromResult(store.GetStore<TEntity>().TryGetValue(key, out var entity) ? entity : null);
        }

        public override Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyCollection<TEntity> results = store.GetStore<TEntity>().Values.ToList();
            return Task.FromResult(results);
        }

        public override Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(store.Add(entity));
        }

        public override Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(store.Add(entity));
        }

        public override Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(store.GetStore<TEntity>().TryRemove(InMemoryTicTacToeStore.ToKey(id), out _));
        }
    }
}
