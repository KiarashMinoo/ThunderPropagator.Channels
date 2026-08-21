using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Groups;

// Issue #136: thrown by GroupService.CreateAsync for an empty name, a resulting membership over
// MaxGroupMembers, or an invited member id that doesn't correspond to an existing user — one
// exception type carrying whichever specific validation message applies, mirroring
// InvalidUserSearchRequestException's (#123) shape.
public
#if !DEBUG
    sealed
#endif
    class InvalidGroupCreateRequestException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
