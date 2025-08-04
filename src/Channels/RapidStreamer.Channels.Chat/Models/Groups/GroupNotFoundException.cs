using System.Net;

namespace RapidStreamer.Channels.Chat.Models.Groups;

public
#if !DEBUG
    sealed
#endif
    class GroupNotFoundException() : HttpRequestException("Group not found", null, HttpStatusCode.NotFound);