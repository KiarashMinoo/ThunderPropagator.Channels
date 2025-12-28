using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Join
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelJoinUserToGroupReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required Guid GroupId
        {
            get => (Guid)this[nameof(GroupId)];
            set => this[nameof(GroupId)] = value;
        }
    }
}