using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ThunderPropagator.Channels.Chat.MongoDB;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.UnitTests.Channels.Chat.MongoDB
{
    /// <summary>
    /// Issue #115: GetContactsAsync's only server round trips are a $or match plus a
    /// SenderId/ReceiverId-only projection (never Body), and a final $in lookup by id — no live
    /// MongoDB server is needed to verify either the match filter reaches the right fields (mirroring
    /// MongoQueryTranslationTests) or the direction/uniqueness logic that turns the projected pairs
    /// into distinct contact ids, which is why that logic is extracted as the internal static
    /// GetDistinctOtherParticipantIds — the same testability pattern MongoDbChatContextIndexTests uses
    /// for index definitions.
    /// </summary>
    public sealed class MongoDbChatContextContactsTests
    {
        public MongoDbChatContextContactsTests() => ChatBsonSerializers.EnsureRegistered();

        [Fact]
        public void ContactMessagesFilter_MatchesOnSenderIdOrReceiverId()
        {
            var userId = Guid.NewGuid();
            FilterDefinition<Message> filter = Builders<Message>.Filter.Or(
                Builders<Message>.Filter.Eq(message => message.SenderId, userId),
                Builders<Message>.Filter.Eq(message => message.ReceiverId, userId));

            var rendered = filter.Render(new RenderArgs<Message>(
                BsonSerializer.LookupSerializer<Message>(),
                BsonSerializer.SerializerRegistry));

            var or = rendered["$or"].AsBsonArray;
            Assert.Contains(or, clause => clause.AsBsonDocument.Contains("SenderId"));
            Assert.Contains(or, clause => clause.AsBsonDocument.Contains("ReceiverId"));
        }

        [Fact]
        public void GetDistinctOtherParticipantIds_WithNoMessages_ReturnsEmpty()
        {
            var userId = Guid.NewGuid();

            var result = MongoDbChatContext.GetDistinctOtherParticipantIds([], userId);

            Assert.Empty(result);
        }

        [Fact]
        public void GetDistinctOtherParticipantIds_WithDuplicateMessagesFromTheSameContact_ReturnsThatContactOnce()
        {
            var userId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var result = MongoDbChatContext.GetDistinctOtherParticipantIds(
                [(contactId, userId), (contactId, userId), (userId, contactId)], userId);

            Assert.Equal([contactId], result);
        }

        [Fact]
        public void GetDistinctOtherParticipantIds_WithOnlySentMessages_IncludesTheReceiver()
        {
            var userId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();

            var result = MongoDbChatContext.GetDistinctOtherParticipantIds([(userId, receiverId)], userId);

            Assert.Equal([receiverId], result);
        }

        [Fact]
        public void GetDistinctOtherParticipantIds_WithOnlyReceivedMessages_IncludesTheSender()
        {
            var userId = Guid.NewGuid();
            var senderId = Guid.NewGuid();

            var result = MongoDbChatContext.GetDistinctOtherParticipantIds([(senderId, userId)], userId);

            Assert.Equal([senderId], result);
        }

        [Fact]
        public void GetDistinctOtherParticipantIds_WithBidirectionalHistory_IncludesEveryDistinctCounterparty()
        {
            var userId = Guid.NewGuid();
            var sentTo = Guid.NewGuid();
            var receivedFrom = Guid.NewGuid();

            var result = MongoDbChatContext.GetDistinctOtherParticipantIds(
                [(userId, sentTo), (receivedFrom, userId)], userId);

            Assert.Equal(2, result.Count);
            Assert.Contains(sentTo, result);
            Assert.Contains(receivedFrom, result);
        }

        // Issue #117: mirrors the pattern above for GetDirectMessageHistoryAsync's match filter —
        // extracted as the internal static GetDirectMessageHistoryFilter so the $and/$or shape and the
        // GroupId exclusion (excluding group-fanned-out rows from a direct conversation) can be
        // verified by rendering, with no live server needed.
        [Fact]
        public void DirectMessageHistoryFilter_ExcludesGroupMessages_AndMatchesEitherDirection()
        {
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var filter = MongoDbChatContext.GetDirectMessageHistoryFilter(userId, otherUserId);
            var rendered = filter.Render(new RenderArgs<Message>(
                BsonSerializer.LookupSerializer<Message>(),
                BsonSerializer.SerializerRegistry));

            // The driver flattens Filter.And(Eq(...), Filter.Or(...)) into one document — an implicit
            // AND of a top-level field and a sibling $or key — rather than an explicit $and array.
            Assert.True(rendered.Contains("GroupId") && rendered["GroupId"].IsBsonNull, $"Expected GroupId: null in {rendered}.");
            var or = rendered["$or"].AsBsonArray;
            Assert.Equal(2, or.Count);
            Assert.Contains(or, clause => clause.AsBsonDocument.Contains("SenderId") && clause["SenderId"].AsGuid == userId);
        }

        [Fact]
        public void GroupMessageHistoryFilter_MatchesOnGroupId()
        {
            var groupId = Guid.NewGuid();
            FilterDefinition<Message> filter = Builders<Message>.Filter.Eq(message => message.GroupId, groupId);

            var rendered = filter.Render(new RenderArgs<Message>(
                BsonSerializer.LookupSerializer<Message>(),
                BsonSerializer.SerializerRegistry));

            Assert.True(rendered.Contains("GroupId"), $"Expected a 'GroupId' field in {rendered}, got none.");
            Assert.Equal(groupId, rendered["GroupId"].AsGuid);
        }
    }
}
