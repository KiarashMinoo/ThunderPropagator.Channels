using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.Channels.Chat.Messages
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelFeederMessage : FeederMessage
    {
        /// <summary>
        /// Creates a new message and captures <see cref="DateTime"/> as of this instant (see that
        /// property's remarks).
        /// </summary>
        public ChatChannelFeederMessage() : this(TimeProvider.System)
        {
        }

        // Issue #138: captured once here rather than left to DateTime's getter — reading DateTime
        // repeatedly must observe the same instant this message was constructed at, not the clock at
        // the moment of each read. Every other constructor below chains to this one so no
        // construction path leaves DateTime unset; the Message-based constructor then overwrites it
        // with the explicitly supplied message.Created, which is exactly the "preserve an explicitly
        // supplied timestamp" case this issue's AC calls for. Accepting a TimeProvider (mirroring
        // NotificationsChannelFeederMessage's own #64/#78 fix for the identical bug) rather than
        // reading TimeProvider.System.GetUtcNow() directly lets a test substitute a fake clock to
        // prove stability deterministically instead of needing an actual wall-clock delay.
        internal ChatChannelFeederMessage(TimeProvider timeProvider)
        {
            DateTime = timeProvider.GetUtcNow();
        }

        // Issue #119: isDeleted lets ChatChannelDeleteMessageReceiverPipeline emit a deletion event
        // through this same feeder-message shape (a channel has exactly one feeder-message type, see
        // CLAUDE.md's mandatory unit structure) rather than a separate type — recipients tell a
        // deletion apart from a new message by this flag, and MessageId (added alongside it, for both
        // cases) by which message it refers to. Issue #120: isEdited does the same for
        // ChatChannelEditMessageReceiverPipeline — Message already carries the revised Body, so
        // recipients just need to know this event is a revision rather than a brand-new message.
        // Issue #125: isRead does the same for read receipts, but — unlike a deletion or edit, which
        // the original recipient needs to know about — a read receipt is addressed to the message's
        // original sender, since they're the one who wants to know their message was read. UserId/
        // SenderUserId are swapped accordingly only when isRead is set; every other field, and every
        // existing Send/Delete/Edit call site (none of which pass isRead), is unaffected.
        internal ChatChannelFeederMessage(Message message, bool isDeleted = false, bool isEdited = false, bool isRead = false) : this(TimeProvider.System)
        {
            MessageId = message.Id;
            UserId = (isRead ? message.SenderId : message.ReceiverId).ToString();
            SenderUserId = isRead ? message.ReceiverId : message.SenderId;
            GroupId = message.GroupId ?? Guid.Empty;
            Message = message.Body;
            DateTime = message.Created;
            IsDeleted = isDeleted;
            IsEdited = isEdited;
            IsRead = isRead;
        }

        // Issue #121: a presence event has no backing Message at all, so it gets its own
        // constructor rather than reusing the one above — recipientUserId is who this specific
        // notification is addressed to (the subscribing key, same role UserId plays for a sent
        // message), and offlineUserId (carried in SenderUserId, the same "whose event is this" role
        // it plays for Send/Delete/Edit) is who just logged out. MessageId/GroupId/Message
        // don't apply to presence and are left at their defaults; DateTime is still captured (via
        // the base TimeProvider constructor above) rather than left unset.
        internal ChatChannelFeederMessage(Guid recipientUserId, Guid offlineUserId) : this(TimeProvider.System)
        {
            UserId = recipientUserId.ToString();
            SenderUserId = offlineUserId;
            IsOffline = true;
        }

        // Issue #124: a group-deletion event has no backing Message either — recipientUserId is
        // each former member being notified, groupId is which group was deleted (the existing
        // GroupId field), and deletedByUserId (carried in SenderUserId, the same role it plays
        // above) is who deleted it — this domain's only admin concept, the group's creator.
        internal ChatChannelFeederMessage(Guid recipientUserId, Guid groupId, Guid deletedByUserId) : this(TimeProvider.System)
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

        /// <summary>
        /// UTC instant this message represents. Captured once, at construction, by every
        /// constructor (see #138) — reading it repeatedly always returns the same value rather than
        /// drifting to the clock at the moment of each read. The Message-based constructor overwrites
        /// this with the message's own <c>Created</c> timestamp, so an explicitly supplied timestamp
        /// is preserved rather than replaced by the construction-time capture.
        /// </summary>
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

        public bool IsRead
        {
            get => GetValueOrDefault(false);
            private set => SetValue(value);
        }
    }
}