namespace ThunderPropagator.Channels.Chat.Endpoints
{
    // Issue #133: a stable "created message" projection covering both targets a send can have. A
    // direct send always persists exactly one Message, so MessageIds has exactly one entry; a group
    // send fans out one Message row per current member (see MessageService.SendMessageToGroupAsync),
    // so MessageIds carries every row created for it — a client that sent one logical group message
    // still gets one response, not one per recipient. SenderId/Body come from the request/caller
    // directly rather than being read back off the persisted rows, so this response stays correct
    // even for a group with zero current members (MessageIds simply empty) instead of needing a
    // "first" row that might not exist.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSentMessageResponseDto
    {
        public required IReadOnlyCollection<Guid> MessageIds { get; init; }
        public required Guid SenderId { get; init; }
        public Guid? ReceiverId { get; init; }
        public Guid? GroupId { get; init; }
        public required string Body { get; init; }
        public required DateTimeOffset Created { get; init; }
    }
}
