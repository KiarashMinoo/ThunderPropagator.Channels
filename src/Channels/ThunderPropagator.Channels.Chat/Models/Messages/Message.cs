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

        public string Body { get; } = null!;

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

        internal static Message Create(Guid senderId, Guid receiverId, string body) => new(senderId, receiverId, body);
        internal static Message Create(Guid senderId, Guid receiverId, Guid groupId, string body) => new(senderId, receiverId, groupId, body);
    }
}