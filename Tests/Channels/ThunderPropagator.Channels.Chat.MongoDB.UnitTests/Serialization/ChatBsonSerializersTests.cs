using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using ThunderPropagator.Channels.Chat.MongoDB;
using ThunderPropagator.Channels.Chat.MongoDB.Serialization;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.MongoDB.Serialization
{
    /// <summary>
    /// Issue #111: User/Group/GroupUser/Message all have private constructors and several get-only
    /// properties, so each has a hand-written serializer (see
    /// Serialization/ChatEntitySerializerBase.cs for why) that constructs the instance through its
    /// private parameterless constructor and reads/writes every field via its compiler-generated
    /// backing field. These tests round-trip real instances through
    /// ToBsonDocument()/BsonSerializer.Deserialize — pure in-memory BSON (de)serialization, no
    /// MongoDB server needed — to prove that actually produces working (de)serialization rather than
    /// a serializer that merely registers without error.
    /// </summary>
    public sealed class ChatBsonSerializersTests
    {
        public ChatBsonSerializersTests() => ChatBsonSerializers.EnsureRegistered();

        [Fact]
        public void User_RoundTripsThroughBson()
        {
            var user = User.Create("alice", "Alice");
            user.SetPasswordHash("hashed-password");

            var document = user.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<User>(document);

            Assert.Equal(user.Id, deserialized.Id);
            Assert.Equal("alice", deserialized.UserName);
            Assert.Equal("hashed-password", deserialized.PasswordHash);
            Assert.Equal("Alice", deserialized.Name);
        }

        [Fact]
        public void User_IdIsStoredUnderTheMongoIdField()
        {
            var user = User.Create("bob", "Bob");
            user.SetPasswordHash("hash");

            var document = user.ToBsonDocument();

            Assert.True(document.Contains("_id"));
            Assert.Equal(user.Id, document["_id"].AsGuid);
        }

        [Fact]
        public void Group_RoundTripsThroughBson()
        {
            var group = Group.Create("Test Group", Guid.NewGuid());

            var document = group.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Group>(document);

            Assert.Equal(group.Id, deserialized.Id);
            Assert.Equal("Test Group", deserialized.Name);
            Assert.Equal(group.CreatedByUserId, deserialized.CreatedByUserId);
            Assert.False(deserialized.IsDeleted);
        }

        [Fact]
        public void GroupUser_RoundTripsThroughBson()
        {
            var groupUser = GroupUser.Create(Guid.NewGuid(), Guid.NewGuid());

            var document = groupUser.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<GroupUser>(document);

            Assert.Equal(groupUser.Id, deserialized.Id);
            Assert.Equal(groupUser.GroupId, deserialized.GroupId);
            Assert.Equal(groupUser.UserId, deserialized.UserId);
        }

        // Issue #142: the navigation-loading contract every IChatContext provider follows —
        // GroupUser.Group/.User are never part of the stored document at all, deliberately (see
        // GroupUserSerializer's own comment); MongoDbChatContext is the one that would populate a
        // navigation after a read (for Message.Sender/Group.GroupUsers, see the matching Message
        // test below), and it never touches GroupUser.Group/.User either. Proven here at the
        // serializer level, since a pure BSON round-trip is what this test file already covers
        // without needing a live MongoDB server.
        [Fact]
        public void GroupUser_RoundTripsThroughBson_WithoutItsGroupOrUserNavigationsPopulated()
        {
            var groupUser = GroupUser.Create(Guid.NewGuid(), Guid.NewGuid());

            var document = groupUser.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<GroupUser>(document);

            Assert.Null(deserialized.Group);
            Assert.Null(deserialized.User);
        }

        // Issue #142: MessageSerializer.Deserialize never sets Sender/Receiver/Group either — see its
        // own comment. Sender is instead populated by MongoDbChatContext.PopulateSendersAsync as a
        // separate step after the driver deserializes the document, mirroring EntityFrameworkCore's
        // AutoInclude and InMemory's InMemoryChatStore.PopulateNavigations; Receiver/Group are never
        // populated by any of the three. This proves the serializer's half of that split — a pure BSON
        // round-trip never reaches MongoDbChatContext's populate step, so a fresh deserialize must
        // leave all three at their unset default regardless of what the real read path does afterward.
        [Fact]
        public void Message_RoundTripsThroughBson_WithoutItsSenderReceiverOrGroupNavigationsPopulated()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hello group");

            var document = message.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Message>(document);

            Assert.Null(deserialized.Sender);
            Assert.Null(deserialized.Receiver);
            Assert.Null(deserialized.Group);
        }

        [Fact]
        public void Message_RoundTripsThroughBson_WithoutAGroup()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");

            var document = message.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Message>(document);

            Assert.Equal(message.Id, deserialized.Id);
            Assert.Equal(message.SenderId, deserialized.SenderId);
            Assert.Equal(message.ReceiverId, deserialized.ReceiverId);
            Assert.Null(deserialized.GroupId);
            Assert.Equal("hello", deserialized.Body);
            Assert.Equal(message.Created, deserialized.Created);
        }

        [Fact]
        public void Message_RoundTripsThroughBson_WithAGroup()
        {
            var groupId = Guid.NewGuid();
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), groupId, "hello group");

            var document = message.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Message>(document);

            Assert.Equal(groupId, deserialized.GroupId);
        }

        [Fact]
        public void Message_RoundTripsThroughBson_BeforeDeletion()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");

            var document = message.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Message>(document);

            Assert.False(deserialized.IsDeleted);
            Assert.Null(deserialized.DeletedAt);
        }

        // Issue #119: soft-delete state round-trips too — a deleted message's redacted Body and
        // DeletedAt must survive serialization the same way every other field does.
        [Fact]
        public void Message_RoundTripsThroughBson_AfterDeletion()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");
            message.MarkDeleted();

            var document = message.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Message>(document);

            Assert.True(deserialized.IsDeleted);
            Assert.Equal(message.DeletedAt, deserialized.DeletedAt);
            Assert.Equal(string.Empty, deserialized.Body);
        }

        [Fact]
        public void Message_RoundTripsThroughBson_BeforeEditing()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");

            var document = message.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Message>(document);

            Assert.False(deserialized.IsEdited);
            Assert.Null(deserialized.EditedAt);
        }

        // Issue #120: edit metadata round-trips too — a revised message's updated Body and EditedAt
        // must survive serialization the same way every other field does.
        [Fact]
        public void Message_RoundTripsThroughBson_AfterEditing()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");
            message.Edit("revised");

            var document = message.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Message>(document);

            Assert.True(deserialized.IsEdited);
            Assert.Equal(message.EditedAt, deserialized.EditedAt);
            Assert.Equal("revised", deserialized.Body);
        }

        [Fact]
        public void Message_RoundTripsThroughBson_BeforeBeingRead()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");

            var document = message.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Message>(document);

            Assert.False(deserialized.IsRead);
            Assert.Null(deserialized.ReadAt);
        }

        // Issue #125: read-receipt state round-trips too — IsRead/ReadAt must survive serialization
        // the same way every other field does.
        [Fact]
        public void Message_RoundTripsThroughBson_AfterBeingRead()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");
            message.MarkRead();

            var document = message.ToBsonDocument();
            var deserialized = BsonSerializer.Deserialize<Message>(document);

            Assert.True(deserialized.IsRead);
            Assert.Equal(message.ReadAt, deserialized.ReadAt);
        }
    }
}
