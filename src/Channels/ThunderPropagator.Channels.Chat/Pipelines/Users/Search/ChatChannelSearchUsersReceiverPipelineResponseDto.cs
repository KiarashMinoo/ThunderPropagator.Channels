using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Search
{
    // Issue #123: reuses #122's ChatChannelGetUserReceiverPipelineResponseDto for each result rather
    // than re-declaring the same reduced projection here — both pipelines live in this same
    // assembly, and "the public profile projection of a User" is one concept, not one per pipeline.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSearchUsersReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required IReadOnlyCollection<ChatChannelGetUserReceiverPipelineResponseDto> Users { get; init; }
        public required int TotalCount { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
    }
}
