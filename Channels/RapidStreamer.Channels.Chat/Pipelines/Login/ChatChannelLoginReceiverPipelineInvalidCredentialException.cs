using System.Net;

namespace RapidStreamer.Channels.Chat.Pipelines.Login
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelLoginReceiverPipelineInvalidCredentialException(Exception? innerException = null)
        : HttpRequestException("Invalid username or password.", innerException, HttpStatusCode.Unauthorized)
    {
    }
}