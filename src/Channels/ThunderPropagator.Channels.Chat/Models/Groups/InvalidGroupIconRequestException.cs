using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Groups;

// Issue #38: thrown by GroupService.SetGroupIconAsync when Icon exceeds
// ChatChannelConfiguration.MaxGroupIconLength — mirrors InvalidGroupCreateRequestException's shape
// (#136), a dedicated type since SetIcon is its own pipeline/operation, not a Create sub-case.
public
#if !DEBUG
    sealed
#endif
    class InvalidGroupIconRequestException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
