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
        // cases) by which message it refers to. Issue #120: isEdited does the same for
        // ChatChannelEditMessageReceiverPipeline — Message already carries the revised Body, so
        // recipients just need to know this event is a revision rather than a brand-new message.
        internal ChatChannelFeederMessage(Message message, bool isDeleted = false, bool isEdited = false)
        {
            MessageId = message.Id;
            UserId = message.ReceiverId.ToString();
            SenderUserId = message.SenderId;
            GroupId = message.GroupId ?? Guid.Empty;
            Message = message.Body;
            DateTime = message.Created;
            IsDeleted = isDeleted;
            IsEdited = isEdited;
        }

        // Issue #121: a presence event has no backing Message at all, so it gets its own
        // constructor rather than reusing the one above — recipientUserId is who this specific
        // notification is addressed to (the subscribing key, same role UserId plays for a sent
        // message), and offlineUserId (carried in SenderUserId, the same "whose event is this" role
        // it plays for Send/Delete/Edit) is who just logged out. MessageId/GroupId/Message/DateTime
        // don't apply to presence and are left at their defaults.
        internal ChatChannelFeederMessage(Guid recipientUserId, Guid offlineUserId)
        {
            UserId = recipientUserId.ToString();
            SenderUserId = offlineUserId;
            IsOffline = true;
        }

        // Issue #124: a group-deletion event has no backing Message either — recipientUserId is
        // each former member being notified, groupId is which group was deleted (the existing
        // GroupId field), and deletedByUserId (carried in SenderUserId, the same role it plays
        // above) is who deleted it — this domain's only admin concept, the group's creator.
        internal ChatChannelFeederMessage(Guid recipientUserId, Guid groupId, Guid deletedByUserId)
        {
            UserId = recipientUserId.ToString();
            SenderUserId = deletedByUserId;
            GroupId = groupId;
            IsGroupDeleted = true;
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

        public bool IsEdited
        {
            get => GetValueOrDefault(false);
            private set => SetValue(value);
        }

        public bool IsOffline
        {
            get => GetValueOrDefault(false);
            private set => SetValue(value);
        }

        public bool IsGroupDeleted
        {
            get => GetValueOrDefault(false);
            private set => SetValue(value);
        }
    }
}