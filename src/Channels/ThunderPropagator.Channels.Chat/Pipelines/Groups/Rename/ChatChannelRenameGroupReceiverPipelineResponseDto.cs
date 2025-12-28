using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Rename
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelRenameGroupReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required Group Group { get; init; }
    }
}