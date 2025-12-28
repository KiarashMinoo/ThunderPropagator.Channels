using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Register
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