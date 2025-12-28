using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Groups;

public
#if !DEBUG
    sealed
#endif
    class GroupNotFoundException() : HttpRequestException("Group not found", null, HttpStatusCode.NotFound);