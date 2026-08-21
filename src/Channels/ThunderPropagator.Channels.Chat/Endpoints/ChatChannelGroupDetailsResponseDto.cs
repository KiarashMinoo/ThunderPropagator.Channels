using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;

namespace ThunderPropagator.Channels.Chat.Endpoints
{
    // Issue #132: a group-details projection rather than the persistence entity — Group itself has
    // no reduced shape today (Groups/GetAll returns raw Group entities), and Members reuses #122's
    // public user projection per member rather than exposing GroupUser/User directly, so nothing
    // sensitive about another member leaks through a group-details call. MemberCount is the true
    // total; Members is only the requested page of it — see GetGroupDetailsAsync's own comment on why
    // paging happens over member ids before any User is looked up, so a large group's cost stays
    // bounded by page size rather than its total membership.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGroupDetailsResponseDto
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public string? GroupIcon { get; init; }
        public required Guid CreatedByUserId { get; init; }
        public required int MemberCount { get; init; }
        public required IReadOnlyCollection<ChatChannelGetUserReceiverPipelineResponseDto> Members { get; init; }
        public required int MembersPage { get; init; }
        public required int MembersPageSize { get; init; }
    }
}
