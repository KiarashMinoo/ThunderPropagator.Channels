using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Update
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