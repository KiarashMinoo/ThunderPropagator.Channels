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
        public User Sender { get; private set; } = null!;

        public Guid ReceiverId { get; }
        public User Receiver { get; private set; } = null!;

        public Guid? GroupId { get; }
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

        internal static Message Create(Guid senderId, Guid receiverId, string body) => new(senderId, receiverId, body);
        internal static Message Create(Guid senderId, Guid receiverId, Guid groupId, string body) => new(senderId, receiverId, groupId, body);
    }
}