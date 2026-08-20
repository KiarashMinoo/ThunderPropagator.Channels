using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.Edit
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelEditMessageReceiverPipelineRequestDto : BindingDictionary<string, object?>, IRequestContentFormCollection
    {
        public required Guid MessageId
        {
            get => (Guid)GetValueOrDefault(nameof(MessageId), Guid.Empty)!;
            set => this[nameof(MessageId)] = value;
        }

        public required string Body
        {
            get => (string)GetValueOrDefault(nameof(Body), string.Empty)!;
            set => this[nameof(Body)] = value;
        }
    }
}
