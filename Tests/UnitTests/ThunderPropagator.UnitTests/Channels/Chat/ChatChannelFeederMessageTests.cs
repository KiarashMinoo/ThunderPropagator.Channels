using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.UnitTests.Channels.Chat
{
    /// <summary>
    /// Issue #119: ChatChannelDeleteMessageReceiverPipeline emits a deletion event through the same
    /// ChatChannelFeederMessage shape a sent message uses (a channel has exactly one feeder-message
    /// type) — MessageId identifies which message an event refers to, and IsDeleted tells a deletion
    /// apart from a new message. Issue #120: ChatChannelEditMessageReceiverPipeline does the same
    /// with IsEdited. All three flags default to their "new message" values when the isDeleted/
    /// isEdited constructor arguments are omitted, so #117/#118's existing send-path call sites
    /// (which never pass either) keep emitting IsDeleted: false, IsEdited: false.
    /// </summary>
    public sealed class ChatChannelFeederMessageTests
    {
        [Fact]
        public void Constructor_WithoutIsDeletedOrIsEdited_DefaultsToANewMessageEvent()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");

            var feederMessage = new ChatChannelFeederMessage(message);

            Assert.False(feederMessage.IsDeleted);
            Assert.False(feederMessage.IsEdited);
            Assert.Equal(message.Id, feederMessage.MessageId);
        }

        [Fact]
        public void Constructor_WithIsDeletedTrue_ProducesADeletionEvent()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");
            message.MarkDeleted();

            var feederMessage = new ChatChannelFeederMessage(message, isDeleted: true);

            Assert.True(feederMessage.IsDeleted);
            Assert.False(feederMessage.IsEdited);
            Assert.Equal(message.Id, feederMessage.MessageId);
        }

        [Fact]
        public void Constructor_WithIsEditedTrue_ProducesAnEditEvent()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");
            message.Edit("revised");

            var feederMessage = new ChatChannelFeederMessage(message, isEdited: true);

            Assert.True(feederMessage.IsEdited);
            Assert.False(feederMessage.IsDeleted);
            Assert.Equal(message.Id, feederMessage.MessageId);
            Assert.Equal("revised", feederMessage.Message);
        }

        [Fact]
        public void Constructor_CarriesTheReceiverIdAsUserId()
        {
            var receiverId = Guid.NewGuid();
            var message = Message.Create(Guid.NewGuid(), receiverId, "hello");

            var feederMessage = new ChatChannelFeederMessage(message);

            Assert.Equal(receiverId.ToString(), feederMessage.UserId);
        }

        // Issue #121: ChatChannelLogoutReceiverPipeline emits a presence event through this same
        // shape via the (recipientUserId, offlineUserId) constructor — no backing Message exists for
        // it at all, unlike Send/Delete/Edit.
        [Fact]
        public void PresenceConstructor_ProducesAnOfflineEvent()
        {
            var recipientId = Guid.NewGuid();
            var offlineUserId = Guid.NewGuid();

            var feederMessage = new ChatChannelFeederMessage(recipientId, offlineUserId);

            Assert.True(feederMessage.IsOffline);
            Assert.False(feederMessage.IsDeleted);
            Assert.False(feederMessage.IsEdited);
            Assert.Equal(recipientId.ToString(), feederMessage.UserId);
            Assert.Equal(offlineUserId, feederMessage.SenderUserId);
        }

        // Issue #124: ChatChannelDeleteGroupReceiverPipeline emits one of these per former group
        // member through this same shape — no backing Message exists for it either, like presence.
        [Fact]
        public void GroupDeletionConstructor_ProducesAGroupDeletedEvent()
        {
            var recipientId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var deletedByUserId = Guid.NewGuid();

            var feederMessage = new ChatChannelFeederMessage(recipientId, groupId, deletedByUserId);

            Assert.True(feederMessage.IsGroupDeleted);
            Assert.False(feederMessage.IsDeleted);
            Assert.False(feederMessage.IsEdited);
            Assert.False(feederMessage.IsOffline);
            Assert.Equal(recipientId.ToString(), feederMessage.UserId);
            Assert.Equal(groupId, feederMessage.GroupId);
            Assert.Equal(deletedByUserId, feederMessage.SenderUserId);
        }
    }
}
