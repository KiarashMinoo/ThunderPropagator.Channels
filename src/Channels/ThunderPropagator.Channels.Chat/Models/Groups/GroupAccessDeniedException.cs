using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Groups;

public
#if !DEBUG
    sealed
#endif
    class GroupAccessDeniedException() : HttpRequestException("You are not a member of this group.", null, HttpStatusCode.Forbidden);
