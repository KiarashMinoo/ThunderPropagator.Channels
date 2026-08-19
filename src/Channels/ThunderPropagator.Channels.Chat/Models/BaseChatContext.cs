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
        // Issue #113: this used to be one static bool shared by every subclass — once ANY concrete
        // provider (EntityFrameworkCoreChatContext, MongoDbChatContext, InMemoryChatContext, ...)
        // initialized, every OTHER provider type would see _isInitialized already true and skip its
        // own Migrate()/Seed() entirely. State is now keyed per concrete type (GetType()), so each
        // provider type tracks its own initialization and its own lock — one type's (possibly slow)
        // migration never blocks or gets confused with another, unrelated type's.
        //
        // Retry policy: a failed Migrate()/Seed() (either throws) leaves that type's state
        // uninitialized — the exception propagates out of the lock before IsInitialized is set, the
        // same as before this fix. The next construction of that same concrete type retries
        // Migrate()+Seed() from scratch, whether that next attempt comes from a thread that was
        // waiting on the lock during the failed attempt or a completely new caller. There is no
        // backoff or circuit breaker: a persistently failing provider (e.g. a genuinely unreachable
        // database) will re-attempt the full Migrate()+Seed() sequence on every single subsequent
        // construction, which callers should account for if that sequence is expensive.
        private static readonly ConcurrentDictionary<Type, InitializationState> InitializationStates = new();

        private sealed class InitializationState
        {
#if NET9_0_OR_GREATER
            public readonly Lock Lock = new();
#else
            public readonly object Lock = new();
#endif
            public volatile bool IsInitialized;
        }

        protected BaseChatContext()
        {
            var state = InitializationStates.GetOrAdd(GetType(), static _ => new InitializationState());
            if (state.IsInitialized)
                return;

            lock (state.Lock)
            {
                if (state.IsInitialized)
                    return;

                Migrate();
                Seed();
                state.IsInitialized = true;
            }
        }

        protected abstract void Migrate();
        protected abstract void Seed();
        public abstract Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        public abstract Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class;
    }
}