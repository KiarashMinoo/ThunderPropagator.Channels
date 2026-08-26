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
    /// Issue #111: "contacts" and "group membership" queries (UserService.GetUserContactsAsync,
    /// GroupService.GetUserGroupsAsync) go through IChatContext.GetAllAsync&lt;TEntity&gt;(predicate),
    /// which MongoDbChatContext hands straight to IMongoCollection&lt;TEntity&gt;.Find(expression) —
    /// for that filter to actually execute in MongoDB rather than being pulled into memory first, the
    /// driver's LINQ provider needs each entity's serializer to implement IBsonDocumentSerializer and
    /// resolve a property name like "ReceiverId" to its BSON element (see
    /// ChatEntitySerializerBase.TryGetMemberSerializationInfo). These tests render the filter to
    /// BsonDocument — no server connection needed — and assert on the resulting field name, proving
    /// the translation actually reaches the database instead of silently falling back to an in-memory
    /// filter (or throwing).
    /// </summary>
    public sealed class MongoQueryTranslationTests
    {
        public MongoQueryTranslationTests() => ChatBsonSerializers.EnsureRegistered();

        [Fact]
        public void MessageReceiverIdFilter_RendersAgainstTheMappedField()
        {
            var receiverId = Guid.NewGuid();
            FilterDefinition<Message> filter = Builders<Message>.Filter.Where(message => message.ReceiverId == receiverId);

            var rendered = filter.Render(new RenderArgs<Message>(
                BsonSerializer.LookupSerializer<Message>(),
                BsonSerializer.SerializerRegistry));

            Assert.True(rendered.Contains("ReceiverId"), $"Expected a 'ReceiverId' field in {rendered}, got none.");
            Assert.Equal(receiverId, rendered["ReceiverId"].AsGuid);
        }

        [Fact]
        public void GroupUserGroupIdFilter_RendersAgainstTheMappedField()
        {
            var groupId = Guid.NewGuid();
            FilterDefinition<GroupUser> filter = Builders<GroupUser>.Filter.Where(groupUser => groupUser.GroupId == groupId);

            var rendered = filter.Render(new RenderArgs<GroupUser>(
                BsonSerializer.LookupSerializer<GroupUser>(),
                BsonSerializer.SerializerRegistry));

            Assert.True(rendered.Contains("GroupId"), $"Expected a 'GroupId' field in {rendered}, got none.");
            Assert.Equal(groupId, rendered["GroupId"].AsGuid);
        }

        [Fact]
        public void UserUserNameFilter_RendersAgainstTheMappedField()
        {
            FilterDefinition<User> filter = Builders<User>.Filter.Where(user => user.UserName == "alice");

            var rendered = filter.Render(new RenderArgs<User>(
                BsonSerializer.LookupSerializer<User>(),
                BsonSerializer.SerializerRegistry));

            Assert.True(rendered.Contains("UserName"), $"Expected a 'UserName' field in {rendered}, got none.");
            Assert.Equal("alice", rendered["UserName"].AsString);
        }

        [Fact]
        public void IdFilter_RendersAgainstTheMongoIdField()
        {
            // Mirrors MongoDbChatContext.ToIdFilterValue: Builders<T>.Filter.Eq("_id", <bare Guid>)
            // resolves the value's serializer generically rather than through the class map, so a
            // bare Guid would use the BSON default GuidSerializer (GuidRepresentation.Unspecified)
            // and throw — wrapping it in BsonBinaryData with the Standard representation is what
            // ChatBsonSerializers registers for every _id, and what the real filter must use too.
            var id = Guid.NewGuid();
            FilterDefinition<User> filter = Builders<User>.Filter.Eq("_id", new BsonBinaryData(id, GuidRepresentation.Standard));

            var rendered = filter.Render(new RenderArgs<User>(
                BsonSerializer.LookupSerializer<User>(),
                BsonSerializer.SerializerRegistry));

            Assert.True(rendered.Contains("_id"));
            Assert.Equal(id, rendered["_id"].AsGuid);
        }
    }
}
