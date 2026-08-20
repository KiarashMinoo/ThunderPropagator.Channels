using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.Delete
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelDeleteMessageReceiverPipelineRequestDto : BindingDictionary<string, object?>, IRequestContentFormCollection
    {
        public required Guid MessageId
        {
            get => (Guid)GetValueOrDefault(nameof(MessageId), Guid.Empty)!;
            set => this[nameof(MessageId)] = value;
        }
    }
}
