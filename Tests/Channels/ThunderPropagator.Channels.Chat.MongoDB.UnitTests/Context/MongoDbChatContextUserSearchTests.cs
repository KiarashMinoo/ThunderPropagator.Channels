using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ThunderPropagator.Channels.Chat.MongoDB;
using ThunderPropagator.Channels.Chat.MongoDB.Context;
using ThunderPropagator.Channels.Chat.MongoDB.Serialization;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.MongoDB.Context
{
    /// <summary>
    /// Issue #123: GetUserSearchFilter is extracted so the $or/regex shape — matching either
    /// UserName or Name, case-insensitively, with the term's regex metacharacters escaped — can be
    /// verified by rendering, mirroring GetDirectMessageHistoryFilter's own testability pattern (#117).
    /// </summary>
    public sealed class MongoDbChatContextUserSearchTests
    {
        public MongoDbChatContextUserSearchTests() => ChatBsonSerializers.EnsureRegistered();

        [Fact]
        public void GetUserSearchFilter_MatchesEitherUserNameOrName_CaseInsensitively()
        {
            var filter = MongoDbChatContext.GetUserSearchFilter("alice");

            var rendered = filter.Render(new RenderArgs<User>(
                BsonSerializer.LookupSerializer<User>(),
                BsonSerializer.SerializerRegistry));

            var or = rendered["$or"].AsBsonArray;
            Assert.Equal(2, or.Count);
            Assert.Contains(or, clause => clause.AsBsonDocument.Contains("UserName"));
            Assert.Contains(or, clause => clause.AsBsonDocument.Contains("Name"));
            foreach (var clause in or)
            {
                var field = clause.AsBsonDocument.Contains("UserName") ? "UserName" : "Name";
                var regex = clause.AsBsonDocument[field].AsBsonRegularExpression;
                Assert.Equal("alice", regex.Pattern);
                Assert.Equal("i", regex.Options);
            }
        }

        [Fact]
        public void GetUserSearchFilter_EscapesRegexMetacharactersInTheTerm()
        {
            var filter = MongoDbChatContext.GetUserSearchFilter("a.b*c");

            var rendered = filter.Render(new RenderArgs<User>(
                BsonSerializer.LookupSerializer<User>(),
                BsonSerializer.SerializerRegistry));

            var or = rendered["$or"].AsBsonArray;
            var userNameClause = or.Select(clause => clause.AsBsonDocument).Single(doc => doc.Contains("UserName"));
            var pattern = userNameClause["UserName"].AsBsonRegularExpression.Pattern;

            Assert.Equal(System.Text.RegularExpressions.Regex.Escape("a.b*c"), pattern);
        }
    }
}
