using RapidStreamer.Application.Collections;
using RapidStreamer.Channels.Chat.Models.Groups;

namespace RapidStreamer.Channels.Chat.Pipelines.Groups.SetIcon
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