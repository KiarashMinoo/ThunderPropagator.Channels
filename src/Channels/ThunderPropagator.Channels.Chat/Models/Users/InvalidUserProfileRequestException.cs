using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Users;

// Issue #38: thrown by UserService.SetNameAsync/SetAvatarAsync/UpdateAsync for a profile field
// (display name, avatar, bio) that is empty where required or exceeds its configured max length —
// shared across the three profile-editing pipelines rather than one exception per field, since
// they're all the same "this profile value is invalid" concern.
public
#if !DEBUG
    sealed
#endif
    class InvalidUserProfileRequestException(string message) : HttpRequestException(message, null, HttpStatusCode.BadRequest);
