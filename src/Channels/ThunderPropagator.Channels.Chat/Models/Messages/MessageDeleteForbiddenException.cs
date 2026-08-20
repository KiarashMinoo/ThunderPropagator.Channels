using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Messages;

// Issue #119: thrown by MessageService.DeleteMessageAsync when the caller isn't the message's
// sender — only the original sender may delete a message.
public
#if !DEBUG
    sealed
#endif
    class MessageDeleteForbiddenException() : HttpRequestException("Only the sender can delete this message.", null, HttpStatusCode.Forbidden);
