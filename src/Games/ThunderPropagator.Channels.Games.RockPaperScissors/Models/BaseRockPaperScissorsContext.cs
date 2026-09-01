namespace ThunderPropagator.Channels.Games.RockPaperScissors.Models
{
    // Issue #288: mirrors ThunderPropagator.Channels.Chat's IChatContext/BaseChatContext — the same
    // multi-provider persistence pattern (InMemory/EntityFrameworkCore/MongoDB), sized to what this
    // module actually needs: two simple entities and one atomicity-sensitive operation, rather than
    // the full generic Update/GetAllAsync-with-predicate surface Chat's five entity types justified.
    internal interface IRockPaperScissorsContext
    {
        Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
        Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;
        Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;

        // Issue #21's original in-memory fix was ConcurrentDictionary.TryAdd — one atomic
        // check-and-claim step so two concurrent callers racing for the same connectionId can never
        // both win. A generic CreateAsync alone can't offer that guarantee portably across providers
        // (a plain insert's uniqueness-violation exception shape differs per provider), so each
        // provider implements this as whatever atomic primitive it actually has (a single locked
        // dictionary operation, a unique index insert with the violation caught, or Mongo's
        // single-document atomicity) instead of a shared find-then-create sequence here.
        Task<bool> TryReserveConnectionAsync(string connectionId, CancellationToken cancellationToken = default);
    }

    public abstract class BaseRockPaperScissorsContext : IRockPaperScissorsContext
    {
        public abstract Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<bool> TryReserveConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

        protected abstract Task MigrateAsync(CancellationToken cancellationToken);
        protected abstract Task SeedAsync(CancellationToken cancellationToken);

        // Mirrors BaseChatContext.InitializeAsync's per-concrete-type, awaitable, retry-on-failure
        // once-only initialization — see that class's own comment for the full reasoning.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, InitializationState> InitializationStates = new();

        private sealed class InitializationState
        {
            public readonly SemaphoreSlim Semaphore = new(1, 1);
            public volatile bool IsInitialized;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var state = InitializationStates.GetOrAdd(GetType(), static _ => new InitializationState());
            if (state.IsInitialized)
                return;

            await state.Semaphore.WaitAsync(cancellationToken);
            try
            {
                if (state.IsInitialized)
                    return;

                await MigrateAsync(cancellationToken);
                await SeedAsync(cancellationToken);
                state.IsInitialized = true;
            }
            finally
            {
                state.Semaphore.Release();
            }
        }
    }
}
