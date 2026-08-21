using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Users;

// Issue #126: thrown by UserService.GetOnlineContactsAsync for out-of-bounds paging — mirrors
// InvalidUserSearchRequestException's shape (#123).
public
#if !DEBUG
    sealed
#endif
    class InvalidOnlineUsersRequestException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
