namespace RapidStreamer.Channels.Chat.Models.Users;

public
#if !DEBUG
    sealed
#endif
    class UserNotFoundException() : HttpRequestException("User not found", null, System.Net.HttpStatusCode.NotFound);