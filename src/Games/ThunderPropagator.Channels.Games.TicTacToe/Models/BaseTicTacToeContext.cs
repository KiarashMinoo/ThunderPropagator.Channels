namespace ThunderPropagator.Channels.Games.TicTacToe.Models
{
    // Mirrors ThunderPropagator.Channels.Games.RockPaperScissors's IRockPaperScissorsContext/BaseRockPaperScissorsContext
    // — the same multi-provider persistence pattern (InMemory/EntityFrameworkCore/MongoDB), sized to
    // this module's one entity. UpdateAsync is the one addition RockPaperScissors's own context didn't
    // need — TicTacToeGameRecord is mutated in place on every move, where RockPaperScissors's records
    // were write-once.
    internal interface ITicTacToeContext
    {
        Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
        Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;
        Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
    }

    public abstract class BaseTicTacToeContext : ITicTacToeContext
    {
        public abstract Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;

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
