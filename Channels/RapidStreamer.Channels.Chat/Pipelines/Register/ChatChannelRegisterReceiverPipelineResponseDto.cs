using RapidStreamer.Application.Collections;
using RapidStreamer.Channels.Chat.Models.Users;

namespace RapidStreamer.Channels.Chat.Pipelines.Register
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelRegisterReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required User User { get; init; }
    }
}