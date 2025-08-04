using RapidStreamer.Application.Collections;
using RapidStreamer.Channels.Chat.Models.Groups;
using RapidStreamer.Channels.Chat.Models.Users;

namespace RapidStreamer.Channels.Chat.Pipelines.Users.Login
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