namespace ThunderPropagator.Channels.Chat.Endpoints
{
    // Issue #133: the REST request body for sending a message — mirrors the WebSocket
    // ChatChannelSendMessageReceiverPipelineRequestDto's target contract (exactly one of
    // ReceiverId/GroupId) but as a plain JSON-bindable type rather than a BindingDictionary, since
    // this is bound from an HTTP request body rather than the WebSocket wire format the pipeline
    // DTOs exist for. SenderId is deliberately not a property here at all — the sender only ever
    // comes from the authenticated principal (see SendMessageAsync), never from anything a client
    // could set in the request body.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSendMessageRequestDto
    {
        public Guid? ReceiverId { get; init; }
        public Guid? GroupId { get; init; }
        public required string Body { get; init; }
    }
}
