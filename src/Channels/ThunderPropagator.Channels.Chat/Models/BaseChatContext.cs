using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ThunderPropagator.Channels.Chat.Models
{
    internal interface IChatContext
    {
        Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class;
        Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
        Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class;
        Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;
        Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
    }

    public abstract class BaseChatContext : IChatContext
    {
        // Issue #114: Migrate()/Seed() used to run synchronously from this class's constructor —
        // every DI-scoped construction of a concrete provider risked blocking a thread on I/O with no
        // way to cancel, and a failing provider surfaced as whatever pipeline happened to construct it
        // first, rather than failing application startup up front. InitializeAsync now runs
        // explicitly instead: ChatContextInitializationHostedService (registered by
        // AddChatChannel<TChatContext>) awaits it during host startup, and since an exception from
        // IHostedService.StartAsync aborts IHost.StartAsync/RunAsync itself, nothing ever resolves a
        // Chat pipeline against an unmigrated/unseeded store. Callers that don't use the generic host
        // may instead call InitializeAsync directly as an explicit startup step.
        //
        // State is still keyed per concrete type (GetType()), carried over from #113: one provider
        // type's initialization must never block or interfere with another's.
        //
        // Retry policy: unchanged in spirit from #113, adapted to be awaitable — a failed
        // MigrateAsync/SeedAsync (either throws) leaves that type's state uninitialized and the
        // exception propagates to the caller. The next InitializeAsync call for that same concrete
        // type retries MigrateAsync+SeedAsync from scratch, whether that call comes from a caller that
        // was waiting on the semaphore during the failed attempt or a completely new one. There is no
        // backoff, circuit breaker, or internal timeout — InitializeAsync only ever honors the
        // CancellationToken its caller passes in.
        private static readonly ConcurrentDictionary<Type, InitializationState> InitializationStates = new();

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

        protected abstract Task MigrateAsync(CancellationToken cancellationToken);
        protected abstract Task SeedAsync(CancellationToken cancellationToken);
        public abstract Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
    }
}