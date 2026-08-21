namespace ThunderPropagator.Channels.Chat.Endpoints
{
    // Issue #135: the REST request body for editing a message — just the revised Body. There is no
    // separate presence/length check here: MessageService.EditMessageAsync already validates it
    // (InvalidMessageEditException for an empty body) after checking ownership and the edit window,
    // the same order the WebSocket Messages/Edit pipeline relies on so a non-sender never gets a
    // validation-shaped response instead of Forbidden.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelEditMessageRequestDto
    {
        public required string Body { get; init; }
    }
}
