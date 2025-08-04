using RapidStreamer.Application.Collections;
using RapidStreamer.Channels.Chat.Models.Groups;
using RapidStreamer.Channels.Chat.Models.Users;

namespace RapidStreamer.Channels.Chat.Pipelines.Users.Update
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUpdateUserReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required User User { get; init; }
    }
}