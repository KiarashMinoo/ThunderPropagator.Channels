using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.MarkRead
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelMarkMessageReadReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required Guid[] MessageIds
        {
            get => (Guid[])this[nameof(MessageIds)];
            set => this[nameof(MessageIds)] = value;
        }
    }
}
