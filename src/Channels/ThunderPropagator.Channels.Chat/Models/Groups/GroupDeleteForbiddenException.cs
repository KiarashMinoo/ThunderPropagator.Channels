using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Groups;

// Issue #124: thrown by GroupService.DeleteGroupAsync when the caller isn't the group's creator —
// this domain's only admin concept (see Group's own comment) — mirroring
// MessageDeleteForbiddenException's shape (#119).
public
#if !DEBUG
    sealed
#endif
    class GroupDeleteForbiddenException() : HttpRequestException("Only the group's creator can delete this group.", null, HttpStatusCode.Forbidden);
