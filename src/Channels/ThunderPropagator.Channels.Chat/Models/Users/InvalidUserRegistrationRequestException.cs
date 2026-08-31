using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Users;

// Issue #38: thrown by UserService.RegisterAsync for an out-of-bounds username, password, or
// display name (empty or over its configured max length) — mirrors InvalidGroupCreateRequestException's
// shape (#136), one exception type carrying whichever specific validation message applies.
public
#if !DEBUG
    sealed
#endif
    class InvalidUserRegistrationRequestException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
