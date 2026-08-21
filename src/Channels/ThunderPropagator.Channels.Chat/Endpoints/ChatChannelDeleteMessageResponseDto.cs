namespace ThunderPropagator.Channels.Chat.Endpoints
{
    // Issue #134: reflects DeleteMessageAsync's own idempotent shape — a fresh delete and a repeat
    // delete of an already-deleted message both return the same IsDeleted=true/DeletedAt result
    // rather than the second call needing to be told apart from the first, matching this issue's own
    // "consistent results ... for repeated requests" AC.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelDeleteMessageResponseDto
    {
        public required Guid MessageId { get; init; }
        public required bool IsDeleted { get; init; }
        public DateTimeOffset? DeletedAt { get; init; }
    }
}
