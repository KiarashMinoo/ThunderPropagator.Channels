using System.Net;

namespace RapidStreamer.Channels.Chat.Exceptions
{
    internal
#if !DEBUG
        sealed
#endif
        class UserNotFoundException(Exception? innerException = null) : HttpRequestException("User not found", innerException, HttpStatusCode.NotFound)
    {
    }
}