using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Messages;

// Issue #120: thrown by MessageService.EditMessageAsync when the revised body fails the same
// presence rule new messages are held to (see MessageService.EditMessageAsync's own comment for why
// this is the only rule that exists to reuse).
public
#if !DEBUG
    sealed
#endif
    class InvalidMessageEditException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
