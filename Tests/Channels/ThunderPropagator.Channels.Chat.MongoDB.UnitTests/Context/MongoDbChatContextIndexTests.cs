using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ThunderPropagator.Channels.Chat.MongoDB;
using ThunderPropagator.Channels.Chat.MongoDB.Context;
using ThunderPropagator.Channels.Chat.MongoDB.Serialization;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.MongoDB.Context
{
    /// <summary>
    /// Issue #111: "Required unique and query indexes are created idempotently." CreateIndexAsync
    /// is idempotent by construction (see MongoDbChatContext.Migrate()'s comment) — what these tests
    /// actually verify, without a live MongoDB server, is that the index *definitions* themselves
    /// have the right key patterns and unique options, by rendering them the same way the driver
    /// would before sending them to a server.
    /// </summary>
    public sealed class MongoDbChatContextIndexTests
    {
        public MongoDbChatContextIndexTests() => ChatBsonSerializers.EnsureRegistered();

        private static BsonDocument RenderKeys<TEntity>(IndexKeysDefinition<TEntity> keys)
            => keys.Render(new RenderArgs<TEntity>(BsonSerializer.LookupSerializer<TEntity>(), BsonSerializer.SerializerRegistry));

        [Fact]
        public void UserNameIndex_IsUniqueOnTheMappedUserNameField()
        {
            var index = MongoDbChatContext.GetUserNameIndex();

            var keys = RenderKeys(index.Keys);

            Assert.True(keys.Contains("UserName"));
            Assert.True(index.Options.Unique);
        }

        [Fact]
        public void GroupUserMembershipIndex_IsUniqueOnGroupIdAndUserId()
        {
            var index = MongoDbChatContext.GetGroupUserMembershipIndex();

            var keys = RenderKeys(index.Keys);

            Assert.True(keys.Contains("GroupId"));
            Assert.True(keys.Contains("UserId"));
            Assert.True(index.Options.Unique);
        }

        [Fact]
        public void MessageIndexes_CoverSenderReceiverAndGroup_AndAreNotUnique()
        {
            var indexes = MongoDbChatContext.GetMessageIndexes();

            var keyFields = indexes.Select(index => RenderKeys(index.Keys).Names.Single()).ToArray();

            Assert.Contains("SenderId", keyFields);
            Assert.Contains("ReceiverId", keyFields);
            Assert.Contains("GroupId", keyFields);
            Assert.All(indexes, index => Assert.False(index.Options?.Unique ?? false));
        }
    }
}
