using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Messages;

// Issue #118: MessageService.GetDirectMessageHistoryAsync/GetGroupMessageHistoryAsync previously
// rejected out-of-bounds Page/PageSize with a plain ArgumentOutOfRangeException, which carries no
// mapped HTTP status the way this domain's other rejections do (see GroupNotFoundException,
// GroupAccessDeniedException) — the AC calls for a deterministic validation response, not whatever
// a generic argument exception happens to translate to.
public
#if !DEBUG
    sealed
#endif
    class InvalidMessageHistoryPageRequestException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
