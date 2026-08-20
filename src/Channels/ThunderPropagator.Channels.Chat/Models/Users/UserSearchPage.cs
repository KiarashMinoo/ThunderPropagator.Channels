namespace ThunderPropagator.Channels.Chat.Models.Users
{
    // Issue #123: mirrors MessageHistoryPage's shape (#117) — the shared return contract for
    // IChatContext.SearchUsersAsync, so every provider paginates server-side and reports how many
    // matching users exist in total. Carries raw User entities rather than a public-profile
    // projection — projecting away PasswordHash/BirthDate is a presentation concern the pipeline
    // handles (see ChatChannelGetUserReceiverPipelineResponseDto.FromUser), not something the
    // provider-level query contract needs to know about.
    public
#if !DEBUG
        sealed
#endif
        class UserSearchPage
    {
        public required IReadOnlyCollection<User> Users { get; init; }
        public required int TotalCount { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
    }
}
