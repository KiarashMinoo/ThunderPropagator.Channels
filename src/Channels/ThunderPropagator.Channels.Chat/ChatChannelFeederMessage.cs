using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.Channels.Chat
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelFeederMessage : FeederMessage
    {
        public ChatChannelFeederMessage()
        {
        }

        // Issue #119: isDeleted lets ChatChannelDeleteMessageReceiverPipeline emit a deletion event
        // through this same feeder-message shape (a channel has exactly one feeder-message type, see
        // CLAUDE.md's mandatory unit structure) rather than a separate type — recipients tell a
        // deletion apart from a new message by this flag, and MessageId (added alongside it, for both
        // cases) by which message it refers to.
        internal ChatChannelFeederMessage(Message message, bool isDeleted = false)
        {
            MessageId = message.Id;
            UserId = message.ReceiverId.ToString();
            SenderUserId = message.SenderId;
            GroupId = message.GroupId ?? Guid.Empty;
            Message = message.Body;
            DateTime = message.Created;
            IsDeleted = isDeleted;
        }

        public Guid MessageId
        {
            get => GetValueOrDefault(Guid.Empty);
            private set => SetValue(value);
        }

        public string UserId
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public Guid SenderUserId
        {
            get => GetValueOrDefault(Guid.Empty);
            private set => SetValue(value);
        }

        public Guid GroupId
        {
            get => GetValueOrDefault(Guid.Empty);
            private set => SetValue(value);
        }

        public string Message
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public DateTimeOffset DateTime
        {
            get => GetValueOrDefault(DateTimeOffset.UtcNow);
            private set => SetValue(value);
        }

        public bool IsDeleted
        {
            get => GetValueOrDefault(false);
            private set => SetValue(value);
        }
    }
}