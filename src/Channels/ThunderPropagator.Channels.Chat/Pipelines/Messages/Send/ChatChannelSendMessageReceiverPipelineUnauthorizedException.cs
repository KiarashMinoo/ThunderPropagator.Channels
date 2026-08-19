using System.Net;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.Send
{
    /// <summary>
    /// Thrown by <see cref="ChatChannelSendMessageReceiverPipeline"/> (see #106) when the calling
    /// connection isn't in <c>ChatChannel.LoggedInUsers</c> — it never logged in, or its session was
    /// removed (e.g. on disconnect) since it did. The message deliberately carries no session
    /// details (no connection id, no prior user id) beyond the fact that the connection isn't
    /// authenticated.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSendMessageReceiverPipelineUnauthorizedException()
        : HttpRequestException("This connection is not authenticated.", null, HttpStatusCode.Unauthorized)
    {
    }
}
