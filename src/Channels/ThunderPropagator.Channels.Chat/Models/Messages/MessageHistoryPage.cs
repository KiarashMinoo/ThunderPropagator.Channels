namespace ThunderPropagator.Channels.Chat.Models.Messages
{
    // Issue #117: the shared return contract for IChatContext.GetDirectMessageHistoryAsync/
    // GetGroupMessageHistoryAsync — every provider paginates server-side and reports how many
    // matching messages exist in total, so a caller can page forward without loading full history
    // or issuing a separate count query.
    public
#if !DEBUG
        sealed
#endif
        class MessageHistoryPage
    {
        public required IReadOnlyCollection<Message> Messages { get; init; }
        public required int TotalCount { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
    }
}
