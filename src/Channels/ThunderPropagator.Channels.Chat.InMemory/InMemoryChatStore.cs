using System.Collections.Concurrent;
using System.Reflection;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.InMemory
{
    /// <summary>
    /// Holds the actual in-memory state for the Chat domain (Users, Groups, GroupUsers, Messages) —
    /// register this as a singleton (see <see cref="InMemoryChatExtensions"/>) and let
    /// <see cref="InMemoryChatContext"/>, which is scoped like every other IChatContext
    /// implementation, wrap it. A fresh scoped context must not mean fresh, empty data.
    ///
    /// A test wanting an isolated store just constructs its own instance directly — <c>new
    /// InMemoryChatStore()</c> — and passes it to <c>new InMemoryChatContext(store)</c>, bypassing DI
    /// entirely. <see cref="Reset"/> and <see cref="Seed{TEntity}"/> exist for exactly that: cheap,
    /// deterministic setup per test, per the AC's "reset/seed support for tests".
    ///
    /// GroupUser lives in its own dictionary rather than embedded in Group.GroupUsers, the same
    /// design #110 (EF Core) and #111 (MongoDB) use — see either provider's own doc comment for why:
    /// a real, globally-correct uniqueness check on (GroupId, UserId) needs GroupUser to be
    /// independently addressable, and Group's own Create/Update has to explicitly insert/reconcile
    /// this dictionary instead of relying on Group's document/row to carry its members.
    ///
    /// Every mutation (Add/Update/Delete, and the Group membership reconciliation they trigger) runs
    /// under a single lock — plain ConcurrentDictionary operations are individually thread-safe, but
    /// "is this username already taken" is a check across every existing User, and a bare
    /// check-then-insert race would let two concurrent registrations both pass the check and both
    /// insert, corrupting the uniqueness guarantee the persistent providers get for free from a real
    /// unique index.
    /// </summary>
    public sealed class InMemoryChatStore
    {
        private static readonly FieldInfo GroupUsersField = typeof(Group)
            .GetField("_groupUsers", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo MessageSenderField = typeof(Message)
            .GetField("<Sender>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!;

#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif

        private readonly ConcurrentDictionary<Guid, User> _users = new();
        private readonly ConcurrentDictionary<Guid, Group> _groups = new();
        private readonly ConcurrentDictionary<Guid, GroupUser> _groupUsers = new();
        private readonly ConcurrentDictionary<Guid, Message> _messages = new();

        /// <summary>Clears every collection — for test isolation between cases sharing one store.</summary>
        public void Reset()
        {
            lock (_lock)
            {
                _users.Clear();
                _groups.Clear();
                _groupUsers.Clear();
                _messages.Clear();
            }
        }

        /// <summary>
        /// Inserts entities directly for deterministic test setup, going through the same uniqueness
        /// checks and Group/GroupUser reconciliation as <see cref="Add{TEntity}"/> so seeded data
        /// can't silently violate the same rules real usage would be stopped from violating.
        /// </summary>
        public void Seed<TEntity>(params TEntity[] entities) where TEntity : class
        {
            foreach (var entity in entities)
                Add(entity);
        }

        internal ConcurrentDictionary<Guid, TEntity> GetStore<TEntity>() where TEntity : class
        {
            if (typeof(TEntity) == typeof(User)) return (ConcurrentDictionary<Guid, TEntity>)(object)_users;
            if (typeof(TEntity) == typeof(Group)) return (ConcurrentDictionary<Guid, TEntity>)(object)_groups;
            if (typeof(TEntity) == typeof(GroupUser)) return (ConcurrentDictionary<Guid, TEntity>)(object)_groupUsers;
            if (typeof(TEntity) == typeof(Message)) return (ConcurrentDictionary<Guid, TEntity>)(object)_messages;

            throw new NotSupportedException($"No store for {typeof(TEntity).Name}.");
        }

        internal static Guid GetId<TEntity>(TEntity entity) where TEntity : class => entity switch
        {
            User user => user.Id,
            Group group => group.Id,
            GroupUser groupUser => groupUser.Id,
            Message message => message.Id,
            _ => throw new NotSupportedException($"No id accessor for {typeof(TEntity).Name}.")
        };

        internal static Guid ToGuid<TPk>(TPk id)
            => id is Guid guid ? guid : throw new NotSupportedException($"Unsupported id type '{typeof(TPk).Name}'.");

        internal TEntity Add<TEntity>(TEntity entity) where TEntity : class
        {
            lock (_lock)
            {
                EnsureUnique(entity);
                GetStore<TEntity>()[GetId(entity)] = entity;

                if (entity is Group group)
                    InsertInitialGroupUsers(group);
            }

            return entity;
        }

        internal TEntity Update<TEntity>(TEntity entity) where TEntity : class
        {
            lock (_lock)
            {
                EnsureUnique(entity);
                GetStore<TEntity>()[GetId(entity)] = entity;

                if (entity is Group group)
                    ReconcileGroupUsers(group);
            }

            return entity;
        }

        internal bool Delete<TEntity, TPk>(TPk id) where TEntity : class
        {
            lock (_lock)
            {
                var targetId = ToGuid(id);
                var removed = GetStore<TEntity>().TryRemove(targetId, out _);

                if (typeof(TEntity) == typeof(Group))
                {
                    foreach (var groupUser in _groupUsers.Values.Where(groupUser => groupUser.GroupId == targetId).ToList())
                        _groupUsers.TryRemove(groupUser.Id, out _);
                }

                return removed;
            }
        }

        internal void PopulateNavigations<TEntity>(TEntity entity) where TEntity : class
        {
            switch (entity)
            {
                case Group group:
                    var set = (HashSet<GroupUser>)GroupUsersField.GetValue(group)!;
                    set.Clear();
                    foreach (var groupUser in _groupUsers.Values.Where(groupUser => groupUser.GroupId == group.Id))
                        set.Add(groupUser);
                    break;
                case Message message:
                    if (_users.TryGetValue(message.SenderId, out var sender))
                        MessageSenderField.SetValue(message, sender);
                    break;
            }
        }

        // Callers must already hold _lock.
        private void EnsureUnique<TEntity>(TEntity entity) where TEntity : class
        {
            switch (entity)
            {
                case User user:
                    if (_users.Values.Any(existing => existing.Id != user.Id
                            && string.Equals(existing.UserName, user.UserName, StringComparison.Ordinal)))
                        throw new InMemoryUniqueConstraintException($"A user with username '{user.UserName}' already exists.");
                    break;
                case GroupUser groupUser:
                    if (_groupUsers.Values.Any(existing => existing.Id != groupUser.Id
                            && existing.GroupId == groupUser.GroupId && existing.UserId == groupUser.UserId))
                        throw new InMemoryUniqueConstraintException(
                            $"User '{groupUser.UserId}' is already a member of group '{groupUser.GroupId}'.");
                    break;
            }
        }

        // Callers must already hold _lock.
        private void InsertInitialGroupUsers(Group group)
        {
            foreach (var groupUser in group.GroupUsers)
            {
                if (_groupUsers.ContainsKey(groupUser.Id))
                    continue;

                EnsureUnique(groupUser);
                _groupUsers[groupUser.Id] = groupUser;
            }
        }

        // Callers must already hold _lock.
        private void ReconcileGroupUsers(Group group)
        {
            var desiredIds = group.GroupUsers.Select(groupUser => groupUser.Id).ToHashSet();
            var currentIds = _groupUsers.Values
                .Where(groupUser => groupUser.GroupId == group.Id)
                .Select(groupUser => groupUser.Id)
                .ToHashSet();

            foreach (var groupUser in group.GroupUsers)
            {
                if (currentIds.Contains(groupUser.Id))
                    continue;

                EnsureUnique(groupUser);
                _groupUsers[groupUser.Id] = groupUser;
            }

            foreach (var idToRemove in currentIds.Except(desiredIds))
                _groupUsers.TryRemove(idToRemove, out _);
        }
    }
}
