using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;

namespace RapidStreamer.Channels.Chat.Pipelines.SendMessage
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSendMessageReceiverPipelineRequestDto : BindingDictionary<string, object?>, IRequestContentFormCollection
    {
        public Guid? ReceiverId
        {
            get => (Guid?)this[nameof(ReceiverId)];
            set => this[nameof(ReceiverId)] = value;
        }

        public Guid? GroupId
        {
            get => (Guid?)this[nameof(ReceiverId)];
            set => this[nameof(ReceiverId)] = value;
        }

        public required string Body
        {
            get => (string)GetValueOrDefault(nameof(Body), string.Empty)!;
            set => this[nameof(Body)] = value;
        }
    }
}