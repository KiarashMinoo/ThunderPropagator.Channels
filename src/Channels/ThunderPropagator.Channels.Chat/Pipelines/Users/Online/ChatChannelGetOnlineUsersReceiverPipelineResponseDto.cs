using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Online
{
    // Issue #126: reuses #122's ChatChannelGetUserReceiverPipelineResponseDto for each result, same
    // as #123's Search response — one public-profile projection, not one per pipeline.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetOnlineUsersReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required IReadOnlyCollection<ChatChannelGetUserReceiverPipelineResponseDto> Users { get; init; }
        public required int TotalCount { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
    }
}
