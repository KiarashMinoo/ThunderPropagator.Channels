using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.RemoveUser
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelRemoveUserToGroupReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required Guid GroupId
        {
            get => (Guid)this[nameof(GroupId)];
            set => this[nameof(GroupId)] = value;
        }

        public required Guid UserId
        {
            get => (Guid)this[nameof(UserId)];
            set => this[nameof(UserId)] = value;
        }
    }
}