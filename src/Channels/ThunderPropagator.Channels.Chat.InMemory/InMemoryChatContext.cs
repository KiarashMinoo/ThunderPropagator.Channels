using System.Linq.Expressions;
using ThunderPropagator.Channels.Chat.Models;

namespace ThunderPropagator.Channels.Chat.InMemory
{
    /// <summary>
    /// Implements the Chat channel's persistence contract (<see cref="BaseChatContext"/>) over a
    /// plain in-process <see cref="InMemoryChatStore"/> — no database, no network, deterministic.
    /// Intended for tests and demos only; see <see cref="InMemoryChatExtensions.AddChatChannel"/> for
    /// why this must never back a real deployment.
    ///
    /// Every read returns a deep clone of the stored entity (via <see cref="InMemoryEntityCloner"/>),
    /// and every write stores a deep clone of what's passed in — the context and the store never
    /// share a live object. Without that, mutating an entity returned from GetAsync would silently
    /// change what's "persisted" without ever calling UpdateAsync, which is exactly the class of bug
    /// a real database (and the EF Core/MongoDB providers backed by one) can't let happen, and which
    /// #112 exists specifically to stop this provider from hiding too.
    /// </summary>
    public sealed class InMemoryChatContext(InMemoryChatStore store) : BaseChatContext
    {
        // Nothing to migrate for a pure in-memory store. This hook also only ever runs once per
        // process (see BaseChatContext), which wouldn't be useful for per-instance test state even
        // if there were something to do here — InMemoryChatStore.Reset()/Seed() are the actual,
        // per-instance test setup mechanism the AC asks for.
        protected override Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compiled = expression.Compile();
            var entity = store.GetStore<TEntity>().Values.FirstOrDefault(compiled);

            return Task.FromResult(entity is null ? null : CloneAndPopulate(entity));
        }

        public override Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!store.GetStore<TEntity>().TryGetValue(InMemoryChatStore.ToGuid(id), out var entity))
                return Task.FromResult<TEntity?>(null);

            return Task.FromResult<TEntity?>(CloneAndPopulate(entity));
        }

        public override Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compiled = expression.Compile();
            IReadOnlyCollection<TEntity> results = store.GetStore<TEntity>().Values
                .Where(compiled)
                .Select(CloneAndPopulate)
                .ToList();

            return Task.FromResult(results);
        }

        public override Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyCollection<TEntity> results = store.GetStore<TEntity>().Values
                .Select(CloneAndPopulate)
                .ToList();

            return Task.FromResult(results);
        }

        public override Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            store.Add(InMemoryEntityCloner.Clone(entity));

            return Task.FromResult(entity);
        }

        public override Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            store.Update(InMemoryEntityCloner.Clone(entity));

            return Task.FromResult(entity);
        }

        public override Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(store.Delete<TEntity, TPk>(id));
        }

        private TEntity CloneAndPopulate<TEntity>(TEntity entity) where TEntity : class
        {
            var clone = InMemoryEntityCloner.Clone(entity);
            store.PopulateNavigations(clone);
            return clone;
        }
    }
}
