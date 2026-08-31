using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Groups;

// Issue #136: thrown by GroupService.CreateAsync for an empty name, a resulting membership over
// MaxGroupMembers, or an invited member id that doesn't correspond to an existing user — one
// exception type carrying whichever specific validation message applies, mirroring
// InvalidUserSearchRequestException's (#123) shape.
//
// Issue #38: also thrown by RenameGroupAsync for the same two name checks (empty, over
// MaxGroupNameLength) — rename's name validation is identical to create's, so it reuses this type
// rather than duplicating it under a rename-specific name.
public
#if !DEBUG
    sealed
#endif
    class InvalidGroupCreateRequestException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
