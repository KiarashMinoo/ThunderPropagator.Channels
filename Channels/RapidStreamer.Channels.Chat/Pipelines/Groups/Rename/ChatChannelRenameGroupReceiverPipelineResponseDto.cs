using RapidStreamer.Application.Collections;
using RapidStreamer.Channels.Chat.Models.Groups;

namespace RapidStreamer.Channels.Chat.Pipelines.Groups.Rename
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