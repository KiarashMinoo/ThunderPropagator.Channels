using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Create
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelCreateGroupReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required Group Group { get; init; }
    }
}