using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using ThunderPropagator.Channels.Chat.Models;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.MongoDB
{
    /// <summary>
    /// Implements the Chat channel's persistence contract (<see cref="BaseChatContext"/>) on top of
    /// the MongoDB C# driver. Unlike EntityFrameworkCore (#110), the driver has no change-tracking or
    /// eager-loading equivalent, so two things EF got "for free" are done explicitly here:
    ///
    /// 1. Group.GroupUsers lives in its own "GroupUsers" collection, not embedded in the Group
    ///    document, so a real, globally-correct unique index on (GroupId, UserId) can enforce
    ///    "can't join a group twice". That means CreateAsync/UpdateAsync for a Group must
    ///    explicitly insert/reconcile its GroupUsers against that separate collection — see
    ///    InsertInitialGroupUsersAsync/ReconcileGroupUsersAsync — since replacing the Group document
    ///    alone would silently drop membership changes GroupService made to the in-memory
    ///    GroupUsers collection.
    /// 2. Group.GroupUsers and Message.Sender (the only two navigations the existing services
    ///    actually read in memory — see MessageService.SendMessageToGroupAsync and
    ///    UserService.GetUserContactsAsync) are populated with a follow-up query after every read,
    ///    mirroring EntityFrameworkCore's AutoInclude but done by hand.
    ///
    /// Transactions: multi-document operations here (e.g. a Group create alongside its initial
    /// GroupUsers) are NOT wrapped in a MongoDB session transaction. IChatContext exposes no
    /// unit-of-work concept for callers to opt into — each service call is a sequence of independent
    /// CreateAsync/UpdateAsync calls with no surrounding transaction boundary available to it either
    /// way — and MongoDB multi-document transactions additionally require a replica set, which is an
    /// operational requirement this package doesn't want to silently impose. A partial failure can
    /// leave, for example, a Group persisted with only some of its initial members inserted; this is
    /// an accepted eventual-consistency trade-off, not an oversight.
    /// </summary>
    public sealed class MongoDbChatContext : BaseChatContext
    {
        private static readonly FieldInfo GroupUsersField = typeof(Group)
            .GetField("_groupUsers", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo MessageSenderField = typeof(Message)
            .GetField("<Sender>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private readonly IMongoDatabase _database;

        public MongoDbChatContext(IMongoDatabase database)
        {
            ChatBsonSerializers.EnsureRegistered();
            _database = database;
        }

        // Extracted from Migrate() so the index definitions themselves — key patterns and unique
        // options — can be unit-tested by rendering them, without needing a live MongoDB server to
        // actually call CreateIndexAsync. CreateIndexAsync/CreateOne is inherently idempotent:
        // creating an index that already exists with the same keys and options is a no-op, so
        // calling this on every Migrate() (itself run at most once per process, see BaseChatContext)
        // is sufficient — no separate "does this index already exist" check is needed.
        internal static CreateIndexModel<User> GetUserNameIndex()
            => new(Builders<User>.IndexKeys.Ascending(user => user.UserName), new CreateIndexOptions { Unique = true });

        internal static CreateIndexModel<GroupUser> GetGroupUserMembershipIndex()
            => new(
                Builders<GroupUser>.IndexKeys.Ascending(groupUser => groupUser.GroupId).Ascending(groupUser => groupUser.UserId),
                new CreateIndexOptions { Unique = true });

        internal static CreateIndexModel<Message>[] GetMessageIndexes() =>
        [
            new(Builders<Message>.IndexKeys.Ascending(message => message.SenderId)),
            new(Builders<Message>.IndexKeys.Ascending(message => message.ReceiverId)),
            new(Builders<Message>.IndexKeys.Ascending(message => message.GroupId))
        ];

        protected override async Task MigrateAsync(CancellationToken cancellationToken)
        {
            await GetCollection<User>().Indexes.CreateOneAsync(GetUserNameIndex(), cancellationToken: cancellationToken);
            await GetCollection<GroupUser>().Indexes.CreateOneAsync(GetGroupUserMembershipIndex(), cancellationToken: cancellationToken);
            await GetCollection<Message>().Indexes.CreateManyAsync(GetMessageIndexes(), cancellationToken);
        }

        // No default seed data — the Chat domain has no fixed reference data to install.
        protected override Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static string GetCollectionName<TEntity>()
        {
            if (typeof(TEntity) == typeof(User)) return "Users";
            if (typeof(TEntity) == typeof(Group)) return "Groups";
            if (typeof(TEntity) == typeof(GroupUser)) return "GroupUsers";
            if (typeof(TEntity) == typeof(Message)) return "Messages";

            throw new NotSupportedException($"No collection mapping for {typeof(TEntity).Name}.");
        }

        private IMongoCollection<TEntity> GetCollection<TEntity>() where TEntity : class
            => _database.GetCollection<TEntity>(GetCollectionName<TEntity>());

        private static Guid GetId<TEntity>(TEntity entity) where TEntity : class => entity switch
        {
            User user => user.Id,
            Group group => group.Id,
            GroupUser groupUser => groupUser.Id,
            Message message => message.Id,
            _ => throw new NotSupportedException($"No id accessor for {typeof(TEntity).Name}.")
        };

        // Builders<TEntity>.Filter.Eq("_id", id) looks up the value's serializer generically rather
        // than through the class map, so a bare Guid falls back to the BSON default GuidSerializer
        // (GuidRepresentation.Unspecified) and throws instead of using the Standard representation
        // ChatBsonSerializers registers for every _id. Every entity's Id is a Guid in this domain, so
        // this always takes the BsonBinaryData path; BsonValue.Create(id) is a defensive fallback for
        // a TPk that never actually occurs today.
        private static BsonValue ToIdFilterValue<TPk>(TPk id)
            => id is Guid guid ? new BsonBinaryData(guid, GuidRepresentation.Standard) : BsonValue.Create(id);

        public override async Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
        {
            var entity = await GetCollection<TEntity>().Find(expression).FirstOrDefaultAsync(cancellationToken);
            if (entity is not null)
                await PopulateNavigationsAsync(entity, cancellationToken);

            return entity;
        }

        public override async Task<TEntity?> GetAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", ToIdFilterValue(id));
            var entity = await GetCollection<TEntity>().Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (entity is not null)
                await PopulateNavigationsAsync(entity, cancellationToken);

            return entity;
        }

        public override async Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default) where TEntity : class
        {
            var entities = await GetCollection<TEntity>().Find(expression).ToListAsync(cancellationToken);
            await PopulateNavigationsAsync(entities, cancellationToken);

            return entities;
        }

        public override async Task<IReadOnlyCollection<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
        {
            var entities = await GetCollection<TEntity>().Find(FilterDefinition<TEntity>.Empty).ToListAsync(cancellationToken);
            await PopulateNavigationsAsync(entities, cancellationToken);

            return entities;
        }

        public override async Task<TEntity> CreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            await GetCollection<TEntity>().InsertOneAsync(entity, cancellationToken: cancellationToken);

            if (entity is Group group)
                await InsertInitialGroupUsersAsync(group, cancellationToken);

            return entity;
        }

        public override async Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", ToIdFilterValue(GetId(entity)));
            await GetCollection<TEntity>().ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true }, cancellationToken);

            if (entity is Group group)
                await ReconcileGroupUsersAsync(group, cancellationToken);

            return entity;
        }

        public override async Task<bool> DeleteAsync<TEntity, TPk>(TPk id, CancellationToken cancellationToken = default) where TEntity : class
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", ToIdFilterValue(id));
            var result = await GetCollection<TEntity>().DeleteOneAsync(filter, cancellationToken);

            if (typeof(TEntity) == typeof(Group) && id is Guid groupId)
                await GetCollection<GroupUser>().DeleteManyAsync(groupUser => groupUser.GroupId == groupId, cancellationToken);

            return result.DeletedCount > 0;
        }

        private async Task InsertInitialGroupUsersAsync(Group group, CancellationToken cancellationToken)
        {
            if (group.GroupUsers.Count == 0)
                return;

            await GetCollection<GroupUser>().InsertManyAsync(group.GroupUsers, cancellationToken: cancellationToken);
        }

        private async Task ReconcileGroupUsersAsync(Group group, CancellationToken cancellationToken)
        {
            var groupUsers = GetCollection<GroupUser>();
            var persisted = await groupUsers.Find(groupUser => groupUser.GroupId == group.Id).ToListAsync(cancellationToken);
            var persistedIds = persisted.Select(groupUser => groupUser.Id).ToHashSet();
            var desiredIds = group.GroupUsers.Select(groupUser => groupUser.Id).ToHashSet();

            var toInsert = group.GroupUsers.Where(groupUser => !persistedIds.Contains(groupUser.Id)).ToList();
            var toDeleteIds = persistedIds.Where(id => !desiredIds.Contains(id)).ToList();

            if (toInsert.Count > 0)
                await groupUsers.InsertManyAsync(toInsert, cancellationToken: cancellationToken);

            if (toDeleteIds.Count > 0)
                await groupUsers.DeleteManyAsync(groupUser => toDeleteIds.Contains(groupUser.Id), cancellationToken);
        }

        private async Task PopulateNavigationsAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class
        {
            switch (entity)
            {
                case Group group:
                    await PopulateGroupUsersAsync([group], cancellationToken);
                    break;
                case Message message:
                    await PopulateSendersAsync([message], cancellationToken);
                    break;
            }
        }

        private async Task PopulateNavigationsAsync<TEntity>(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken) where TEntity : class
        {
            if (entities.Count == 0)
                return;

            if (typeof(TEntity) == typeof(Group))
                await PopulateGroupUsersAsync(entities.Cast<Group>().ToArray(), cancellationToken);
            else if (typeof(TEntity) == typeof(Message))
                await PopulateSendersAsync(entities.Cast<Message>().ToArray(), cancellationToken);
        }

        private async Task PopulateGroupUsersAsync(IReadOnlyCollection<Group> groups, CancellationToken cancellationToken)
        {
            var groupIds = groups.Select(group => group.Id).ToArray();
            var groupUsers = await GetCollection<GroupUser>()
                .Find(groupUser => groupIds.Contains(groupUser.GroupId))
                .ToListAsync(cancellationToken);
            var byGroupId = groupUsers.ToLookup(groupUser => groupUser.GroupId);

            foreach (var group in groups)
            {
                var set = (HashSet<GroupUser>)GroupUsersField.GetValue(group)!;
                set.Clear();
                foreach (var groupUser in byGroupId[group.Id])
                    set.Add(groupUser);
            }
        }

        private async Task PopulateSendersAsync(IReadOnlyCollection<Message> messages, CancellationToken cancellationToken)
        {
            var senderIds = messages.Select(message => message.SenderId).Distinct().ToArray();
            var senders = await GetCollection<User>()
                .Find(user => senderIds.Contains(user.Id))
                .ToListAsync(cancellationToken);
            var byId = senders.ToDictionary(user => user.Id);

            foreach (var message in messages)
            {
                if (byId.TryGetValue(message.SenderId, out var sender))
                    MessageSenderField.SetValue(message, sender);
            }
        }
    }
}
