using System.Net;
using ThunderPropagator.Channels.Chat.Channel;

namespace ThunderPropagator.Channels.Chat.Pipelines
{
    /// <summary>
    /// Thrown by <see cref="AuthenticatedChatChannelReceiverPipeline"/> (see #109) when the calling
    /// connection has no persisted session (see #46's ChatUserSessionService) — it never logged in,
    /// or its session was removed (e.g. on disconnect) since it did. The message deliberately carries
    /// no session details
    /// (no connection id, no prior user id) beyond the fact that the connection isn't authenticated,
    /// so every protected pipeline returns the same documented response contract on rejection.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUnauthorizedException()
        : HttpRequestException("This connection is not authenticated.", null, HttpStatusCode.Unauthorized)
    {
    }
}
