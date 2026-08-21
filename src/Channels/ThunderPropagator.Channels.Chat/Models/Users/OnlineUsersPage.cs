namespace ThunderPropagator.Channels.Chat.Models.Users
{
    // Issue #126: mirrors UserSearchPage's shape (#123) — the return contract for
    // UserService.GetOnlineContactsAsync. Carries raw User entities rather than a public-profile
    // projection, for the same reason UserSearchPage does: projecting away private fields is the
    // pipeline's job (see ChatChannelGetUserReceiverPipelineResponseDto.FromUser), not this
    // contract's.
    public
#if !DEBUG
        sealed
#endif
        class OnlineUsersPage
    {
        public required IReadOnlyCollection<User> Users { get; init; }
        public required int TotalCount { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
    }
}
