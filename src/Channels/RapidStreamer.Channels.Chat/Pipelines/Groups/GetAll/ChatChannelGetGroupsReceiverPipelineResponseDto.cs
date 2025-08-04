using RapidStreamer.Application.Collections;
using RapidStreamer.Channels.Chat.Models.Groups;

namespace RapidStreamer.Channels.Chat.Pipelines.Groups.GetAll
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