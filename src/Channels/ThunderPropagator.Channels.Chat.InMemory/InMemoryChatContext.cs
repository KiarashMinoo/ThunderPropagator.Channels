using System.Linq.Expressions;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

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

        // Issue #115: reads SenderId/ReceiverId straight off the stored Message entries — never
        // Message.Sender/Receiver — so no navigation population is needed to build the contact list.
        public override Task<IReadOnlyCollection<User>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var otherUserIds = store.GetStore<Message>().Values
                .Where(message => message.SenderId == userId || message.ReceiverId == userId)
                .Select(message => message.SenderId == userId ? message.ReceiverId : message.SenderId)
                .Distinct();

            IReadOnlyCollection<User> results = otherUserIds
                .Select(id => store.GetStore<User>().TryGetValue(id, out var user) ? user : null)
                .Where(user => user is not null)
                .Select(user => InMemoryEntityCloner.Clone(user!))
                .ToList();

            return Task.FromResult(results);
        }

        // Issue #117: GroupId is null excludes group-fanned-out rows from a direct conversation, even
        // when one happens to name the same two users as sender/receiver. Issue #119: !IsDeleted
        // excludes soft-deleted messages from history entirely, rather than returning them redacted.
        public override Task<MessageHistoryPage> GetDirectMessageHistoryAsync(Guid userId, Guid otherUserId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matches = store.GetStore<Message>().Values.Where(message => message.GroupId is null && !message.IsDeleted &&
                ((message.SenderId == userId && message.ReceiverId == otherUserId) ||
                 (message.SenderId == otherUserId && message.ReceiverId == userId)));

            return Task.FromResult(BuildHistoryPage(matches, page, pageSize));
        }

        public override Task<MessageHistoryPage> GetGroupMessageHistoryAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matches = store.GetStore<Message>().Values.Where(message => message.GroupId == groupId && !message.IsDeleted);

            return Task.FromResult(BuildHistoryPage(matches, page, pageSize));
        }

        private MessageHistoryPage BuildHistoryPage(IEnumerable<Message> matches, int page, int pageSize)
        {
            var ordered = matches
                .OrderByDescending(message => message.Created)
                .ThenByDescending(message => message.Id)
                .ToList();

            var messages = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(CloneAndPopulate)
                .ToList();

            return new MessageHistoryPage { Messages = messages, TotalCount = ordered.Count, Page = page, PageSize = pageSize };
        }

        // Issue #123: normalizedTerm is already lowercased by UserService — ToLowerInvariant on both
        // UserName/Name keeps the match case-insensitive.
        public override Task<UserSearchPage> SearchUsersAsync(string normalizedTerm, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matches = store.GetStore<User>().Values.Where(user =>
                user.UserName.ToLowerInvariant().Contains(normalizedTerm) || user.Name.ToLowerInvariant().Contains(normalizedTerm));

            var ordered = matches
                .OrderBy(user => user.UserName, StringComparer.Ordinal)
                .ThenBy(user => user.Id)
                .ToList();

            var users = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(CloneAndPopulate)
                .ToList();

            return Task.FromResult(new UserSearchPage { Users = users, TotalCount = ordered.Count, Page = page, PageSize = pageSize });
        }

        private TEntity CloneAndPopulate<TEntity>(TEntity entity) where TEntity : class
        {
            var clone = InMemoryEntityCloner.Clone(entity);
            store.PopulateNavigations(clone);
            return clone;
        }
    }
}
