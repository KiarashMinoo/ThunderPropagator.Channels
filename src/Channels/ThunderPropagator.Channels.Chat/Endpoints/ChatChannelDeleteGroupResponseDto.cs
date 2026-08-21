namespace ThunderPropagator.Channels.Chat.Endpoints
{
    // Issue #137: mirrors ChatChannelDeleteMessageResponseDto's idempotent shape (#134) — a fresh
    // delete and a repeat delete by the creator of an already-deleted group both return the same
    // IsDeleted=true/DeletedAt result, matching GroupService.DeleteGroupAsync's own idempotent
    // contract (#124).
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelDeleteGroupResponseDto
    {
        public required Guid GroupId { get; init; }
        public required bool IsDeleted { get; init; }
        public DateTimeOffset? DeletedAt { get; init; }
    }
}
