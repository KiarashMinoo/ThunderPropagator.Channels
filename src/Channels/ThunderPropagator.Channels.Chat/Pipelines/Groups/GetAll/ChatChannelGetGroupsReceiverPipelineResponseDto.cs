using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.GetAll
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetGroupsReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required IReadOnlyCollection<Group> Groups { get; init; }
    }
}