using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Users;

// Issue #123: thrown by UserService.SearchUsersAsync for an out-of-bounds search term (empty, too
// short, or too long) or out-of-bounds paging — mirrors InvalidMessageHistoryPageRequestException's
// shape (#117/#118), one exception type carrying whichever specific validation message applies.
public
#if !DEBUG
    sealed
#endif
    class InvalidUserSearchRequestException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
