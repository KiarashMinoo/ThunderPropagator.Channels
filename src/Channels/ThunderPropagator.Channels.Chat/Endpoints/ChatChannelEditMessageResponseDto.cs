namespace ThunderPropagator.Channels.Chat.Endpoints
{
    // Issue #135: the updated-message projection the AC asks for. ReceiverId/GroupId mirror #133's
    // send response convention (only the target the message was actually addressed as is populated)
    // rather than exposing the group-fan-out row's own ReceiverId alongside GroupId, which would
    // read as though the edit had a direct recipient in addition to the group.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelEditMessageResponseDto
    {
        public required Guid MessageId { get; init; }
        public required Guid SenderId { get; init; }
        public Guid? ReceiverId { get; init; }
        public Guid? GroupId { get; init; }
        public required string Body { get; init; }
        public required DateTimeOffset Created { get; init; }
        public required bool IsEdited { get; init; }
        public DateTimeOffset? EditedAt { get; init; }
    }
}
