using System.Collections.Concurrent;
using System.Linq.Expressions;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

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

        // Issue #115: UserService.GetUserContactsAsync used to load every Message the user received
        // via the generic GetAllAsync<Message> and project distinct senders in memory — a cost that
        // scales with conversation history and only ever considered messages received, not sent. Each
        // provider implements this as its own server-side, distinct-by-the-other-party projection
        // (see each GetContactsAsync override for how) instead of routing through the generic entity
        // methods. A "contact" is anyone the user has exchanged a direct or group-fanned-out message
        // with in EITHER direction (sent to or received from) — not received-only.
        Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Issue #117: direct and group message history are each their own server-side, paginated,
        // deterministically-ordered query — like GetContactsAsync above, routing this through the
        // generic GetAllAsync<Message> would force every provider to load the complete conversation
        // into memory just to page or count it. "Direct" excludes group-fanned-out rows (GroupId is
        // null) so a 1:1 conversation never surfaces a group broadcast that happens to name the same
        // two users. Page is 1-based; PageSize bounds are validated by MessageService before either
        // method is called, so providers can assume both are already in range.
        Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid userId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default);

        // Issue #123: a server-side, paginated, deterministically-ordered (UserName, then Id as a
        // tiebreaker) case-insensitive substring match against UserName or Name — same reasoning as
        // GetContactsAsync/the message history queries above: routing this through GetAllAsync<User>
        // would force every provider to load every user just to search or count them.
        // normalizedTerm is already trimmed and bounds-checked by UserService before this is called,
        // so providers can assume it's non-empty and within range; case-folding for matching is each
        // provider's own concern (SQL collation, Mongo regex options, or plain string comparison).
        Task<UserSearchPage> SearchUsersAsync(string normalizedTerm, int page, int pageSize, CancellationToken cancellationToken = default);
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
        public abstract Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default);
        public abstract Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid userId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default);
        public abstract Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default);
        public abstract Task<UserSearchPage> SearchUsersAsync(string normalizedTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    }
}