using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Login
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelLoginReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required User User { get; init; }
        public required IReadOnlyCollection<Group> Groups { get; init; }
        public required IReadOnlyCollection<User> Contacts { get; init; }
    }
}