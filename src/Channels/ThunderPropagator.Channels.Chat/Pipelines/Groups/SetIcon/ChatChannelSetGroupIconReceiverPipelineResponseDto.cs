using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.SetIcon
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSetGroupIconReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required Group Group { get; init; }
    }
}