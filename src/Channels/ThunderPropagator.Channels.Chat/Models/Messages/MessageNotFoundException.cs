using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Messages;

public
#if !DEBUG
    sealed
#endif
    class MessageNotFoundException() : HttpRequestException("Message not found", null, HttpStatusCode.NotFound);
