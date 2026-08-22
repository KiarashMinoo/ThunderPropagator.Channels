using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.Models.Messages
{
    public
#if !DEBUG
        sealed
#endif
        class Message
    {
        public Guid Id { get; }
        public Guid SenderId { get; }

        // Issue #142: the one navigation property every IChatContext provider guarantees populated
        // after any read (GetAsync/GetAllAsync) — see EntityFrameworkCore's MessageConfiguration
        // (AutoInclude), MongoDB's MongoDbChatContext.PopulateSendersAsync, and InMemory's
        // InMemoryChatStore.PopulateNavigations, all three of which resolve it from SenderId the same
        // way. Kept populated for any consumer that reads a Message directly and displays its sender;
        // GetContactsAsync (#115) doesn't rely on this — it projects SenderId/ReceiverId server-side
        // instead, without touching this navigation at all.
        public User Sender { get; private set; } = null!;

        public Guid ReceiverId { get; }

        // Issue #142: not part of any provider's loading contract, unlike Sender — no provider
        // intentionally populates it (there's no equivalent consumer need; nothing displays "who
        // received this" the way Sender's "who sent this" is displayed), so it's null in the common
        // case. EntityFrameworkCore's change-tracking could in principle fix this reference up if the
        // matching User happened to already be tracked in the same DbContext for an unrelated reason,
        // the same incidental mechanism documented on GroupUser.Group — never rely on this being
        // either null or populated; resolve the receiver via ReceiverId
        // (e.g. IChatContext.GetAsync<User, Guid>) if actually needed.
        public User? Receiver { get; private set; }

        public Guid? GroupId { get; }

        // Issue #142: same "not part of the contract" status as Receiver above, independently of
        // GroupId being set — a direct message (GroupId null) obviously can't have one, but a group
        // message's Group is null in the common case too. Resolve it via GroupId if needed.
        public Group? Group { get; private set; }

        public DateTimeOffset Created { get; }

        public string Body { get; private set; } = null!;

        // Issue #119: soft-delete state. A deleted message keeps its row (so a repeated delete
        // request can be told apart from one for a message that never existed) but has its Body
        // redacted, so deleted content is never re-exposed through any read path that forgets to
        // filter IsDeleted — GetDirectMessageHistoryAsync/GetGroupMessageHistoryAsync exclude deleted
        // messages entirely rather than relying on every consumer to check this flag itself.
        public bool IsDeleted { get; private set; }
        public DateTimeOffset? DeletedAt { get; private set; }

        // Issue #120: edit metadata. EditedAt is set on every edit (not just the first), so it always
        // reflects the most recent revision.
        public bool IsEdited { get; private set; }
        public DateTimeOffset? EditedAt { get; private set; }

        // Issue #125: read-receipt state. Idempotent by design, same reasoning as MarkDeleted below
        // — a second call is a no-op rather than overwriting ReadAt, so read state can never regress
        // back to unread once set, including under a genuine concurrent race (see
        // MessageService.MarkMessagesReadAsync's own comment).
        public bool IsRead { get; private set; }
        public DateTimeOffset? ReadAt { get; private set; }

        private Message()
        {
            Id = Guid.NewGuid();
            Created = DateTimeOffset.UtcNow;
        }

        private Message(Guid senderId, Guid receiverId, string body) : this()
        {
            SenderId = senderId;
            ReceiverId = receiverId;
            Body = body;
        }

        private Message(Guid senderId, Guid receiverId, Guid groupId, string body) : this(senderId, receiverId, body)
        {
            GroupId = groupId;
        }

        // Idempotent by design: a second call is a no-op rather than overwriting DeletedAt. Defense
        // in depth alongside MessageService.DeleteMessageAsync's own "already deleted" short-circuit
        // (which skips the write/notification entirely on a repeat call) — this guarantees the same
        // safety even if some future caller invokes MarkDeleted directly.
        internal Message MarkDeleted()
        {
            if (IsDeleted)
                return this;

            IsDeleted = true;
            DeletedAt = DateTimeOffset.UtcNow;
            Body = string.Empty;
            return this;
        }

        // Unlike MarkDeleted, not idempotent by design — every call legitimately revises content, so
        // a second edit within the window is expected to update EditedAt again.
        internal Message Edit(string body)
        {
            Body = body;
            IsEdited = true;
            EditedAt = DateTimeOffset.UtcNow;
            return this;
        }

        // Idempotent by design, mirroring MarkDeleted: a second call is a no-op rather than
        // overwriting ReadAt.
        internal Message MarkRead()
        {
            if (IsRead)
                return this;

            IsRead = true;
            ReadAt = DateTimeOffset.UtcNow;
            return this;
        }

        internal static Message Create(Guid senderId, Guid receiverId, string body) => new(senderId, receiverId, body);
        internal static Message Create(Guid senderId, Guid receiverId, Guid groupId, string body) => new(senderId, receiverId, groupId, body);
    }
}