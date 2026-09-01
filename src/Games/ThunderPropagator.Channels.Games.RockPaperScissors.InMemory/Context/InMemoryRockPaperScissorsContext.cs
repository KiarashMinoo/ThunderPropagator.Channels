using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.InMemory.Context
{
    /// <summary>
    /// Implements the RockPaperScissors persistence contract over a plain in-process
    /// <see cref="InMemoryRockPaperScissorsStore"/> — no database, no network, deterministic.
    /// Intended for tests and demos only; mirrors ThunderPropagator.Channels.Chat.InMemory's own
    /// InMemoryChatContext.
    /// </summary>
    public sealed class InMemoryRockPaperScissorsContext(InMemoryRockPaperScissorsStore store) : BaseRockPaperScissorsContext
    {
        protected override Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = InMemoryRockPaperScissorsStore.ToKey(id);
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

        public override Task<bool> TryReserveConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(store.TryReserveConnection(connectionId));
        }
    }
}
