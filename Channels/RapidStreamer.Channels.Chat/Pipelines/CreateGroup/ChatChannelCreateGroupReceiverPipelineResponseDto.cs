using RapidStreamer.Application.Collections;
using RapidStreamer.Channels.Chat.Models.Groups;

namespace RapidStreamer.Channels.Chat.Pipelines.CreateGroup
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