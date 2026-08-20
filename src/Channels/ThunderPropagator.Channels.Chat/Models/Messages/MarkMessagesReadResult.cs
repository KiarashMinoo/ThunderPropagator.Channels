namespace ThunderPropagator.Channels.Chat.Models.Messages
{
    // Issue #125: MessageService.MarkMessagesReadAsync's result — every requested id is resolved
    // independently (see that method's own comment for why), so the caller needs both which messages
    // were actually marked read and which ids failed, rather than one exception for the whole batch.
    public
#if !DEBUG
        sealed
#endif
        class MarkMessagesReadResult
    {
        public required IReadOnlyCollection<Message> MarkedRead { get; init; }
        public required IReadOnlyCollection<Guid> FailedMessageIds { get; init; }
    }
}
