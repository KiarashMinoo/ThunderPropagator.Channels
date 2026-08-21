using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.Channels.Chat.Endpoints
{
    // Issue #131: a purpose-built reduced projection for the REST group-listing endpoint. Unlike
    // Users/Get and Users/Search (#122/#123), no existing WebSocket pipeline already returns a
    // reduced Group projection to reuse — Groups/GetAll returns raw Group entities, GroupUsers (each
    // carrying a nested User) and all. Exposing that here would leak every other member's profile
    // through a simple "list my groups" call; MemberCount is the one fact about membership this
    // summary needs.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGroupSummaryDto
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public string? GroupIcon { get; init; }
        public required Guid CreatedByUserId { get; init; }
        public required int MemberCount { get; init; }

        internal static ChatChannelGroupSummaryDto FromGroup(Group group) => new()
        {
            Id = group.Id,
            Name = group.Name,
            GroupIcon = group.GroupIcon,
            CreatedByUserId = group.CreatedByUserId,
            MemberCount = group.GroupUsers.Count
        };
    }

    // Issue #131: mirrors the {items, TotalCount, Page, PageSize} envelope every other paginated REST
    // response in this surface uses (Users/Search, direct/group message history), for consistency
    // even though a single user's own group memberships are bounded enough that UserService.GetUserGroupsAsync
    // doesn't paginate at the provider level the way message/user history do.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetGroupsResponseDto
    {
        public required IReadOnlyCollection<ChatChannelGroupSummaryDto> Groups { get; init; }
        public required int TotalCount { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
    }
}
