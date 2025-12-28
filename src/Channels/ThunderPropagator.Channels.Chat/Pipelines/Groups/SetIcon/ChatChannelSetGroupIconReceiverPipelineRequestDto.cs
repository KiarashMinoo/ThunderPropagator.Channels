using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.SetIcon
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSetGroupIconReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required Guid GroupId
        {
            get => (Guid)this[nameof(GroupId)];
            set => this[nameof(GroupId)] = value;
        }

        public required string Icon
        {
            get => (string)this[nameof(Icon)];
            set => this[nameof(Icon)] = value;
        }
    }
}