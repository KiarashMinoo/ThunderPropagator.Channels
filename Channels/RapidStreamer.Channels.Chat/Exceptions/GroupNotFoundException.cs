using System.Net;

namespace RapidStreamer.Channels.Chat.Exceptions
{
    internal
#if !DEBUG
        sealed
#endif
        class GroupNotFoundException(Exception? innerException = null) : HttpRequestException("Group not found", innerException, HttpStatusCode.NotFound)
    {
    }
}