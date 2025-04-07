using RapidStreamer.BuildingBlocks.Application;
using RapidStreamer.Channels.Chat.Models.Messages;

namespace RapidStreamer.Channels.Chat
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

        internal ChatChannelFeederMessage(Message message)
        {
            UserId = message.ReceiverId.ToString();
            SenderUserId = message.SenderId;
            GroupId = message.GroupId ?? Guid.Empty;
            Message = message.Body;
            DateTime = message.Created;
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
    }
}