using System.Net;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.Channels.Chat.Models.Messages;

// Issue #141: thrown by MessageService.SendMessageAsync/SendMessageToGroupAsync when Body exceeds
// ChatChannelConfiguration.MaxMessageLength — mirrors InvalidMessageEditException's shape (#120)
// so both a rejected send and a rejected edit map to the same BadRequest contract.
public
#if !DEBUG
    sealed
#endif
    class InvalidMessageSendException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
