using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.UnitTests.Channels.Chat
{
    /// <summary>
    /// Issue #119: ChatChannelDeleteMessageReceiverPipeline emits a deletion event through the same
    /// ChatChannelFeederMessage shape a sent message uses (a channel has exactly one feeder-message
    /// type) — MessageId identifies which message an event refers to, and IsDeleted tells a deletion
    /// apart from a new message. Both default to their "new message" values when the isDeleted
    /// constructor argument is omitted, so #117/#118's existing send-path call sites (which never
    /// pass it) keep emitting IsDeleted: false.
    /// </summary>
    public sealed class ChatChannelFeederMessageTests
    {
        [Fact]
        public void Constructor_WithoutIsDeleted_DefaultsToANewMessageEvent()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");

            var feederMessage = new ChatChannelFeederMessage(message);

            Assert.False(feederMessage.IsDeleted);
            Assert.Equal(message.Id, feederMessage.MessageId);
        }

        [Fact]
        public void Constructor_WithIsDeletedTrue_ProducesADeletionEvent()
        {
            var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hello");
            message.MarkDeleted();

            var feederMessage = new ChatChannelFeederMessage(message, isDeleted: true);

            Assert.True(feederMessage.IsDeleted);
            Assert.Equal(message.Id, feederMessage.MessageId);
        }

        [Fact]
        public void Constructor_CarriesTheReceiverIdAsUserId()
        {
            var receiverId = Guid.NewGuid();
            var message = Message.Create(Guid.NewGuid(), receiverId, "hello");

            var feederMessage = new ChatChannelFeederMessage(message);

            Assert.Equal(receiverId.ToString(), feederMessage.UserId);
        }
    }
}
