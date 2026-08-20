using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Messages;

// Issue #120: thrown by MessageService.EditMessageAsync when the caller isn't the message's sender
// — only the original sender may edit a message, mirroring MessageDeleteForbiddenException (#119).
public
#if !DEBUG
    sealed
#endif
    class MessageEditForbiddenException() : HttpRequestException("Only the sender can edit this message.", null, HttpStatusCode.Forbidden);
