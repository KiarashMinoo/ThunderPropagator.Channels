using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Rename
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelRenameGroupReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required Guid GroupId
        {
            get => (Guid)this[nameof(GroupId)];
            set => this[nameof(GroupId)] = value;
        }

        public required string Name
        {
            get => (string)this[nameof(Name)];
            set => this[nameof(Name)] = value;
        }
    }
}