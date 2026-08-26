using System.Net;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.Channels.Chat.Models.Messages;

// Issue #120: thrown by MessageService.EditMessageAsync once ChatChannelConfiguration.MessageEditWindow
// has elapsed since the message was sent — even the original sender can no longer edit it.
public
#if !DEBUG
    sealed
#endif
    class MessageEditWindowExpiredException() : HttpRequestException("The edit window for this message has expired.", null, HttpStatusCode.Forbidden);
