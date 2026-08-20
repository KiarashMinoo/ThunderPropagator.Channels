using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Delete
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelDeleteGroupReceiverPipelineRequestDto : BindingDictionary<string, object?>, IRequestContentFormCollection
    {
        public required Guid GroupId
        {
            get => (Guid)GetValueOrDefault(nameof(GroupId), Guid.Empty)!;
            set => this[nameof(GroupId)] = value;
        }
    }
}
