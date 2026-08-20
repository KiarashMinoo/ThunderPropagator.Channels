using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Get
{
    // Issue #122: a deliberately reduced projection of User rather than the persistence entity
    // itself — unlike ChatChannelUpdateUserReceiverPipelineResponseDto/the Login response (both a
    // caller's own profile, where returning the whole entity is fine, PasswordHash aside), this
    // pipeline can return any user's profile, including a stranger's. PasswordHash is excluded (as
    // it already is everywhere via [JsonIgnore], but this type never even has the property to leak
    // through a serializer that didn't respect the attribute), and so is BirthDate — the one other
    // field on User with an obvious privacy expectation for a profile someone else looks up. Bio is
    // kept: it's user-authored, intentionally-public "about me" content, not private data.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetUserReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required Guid Id { get; init; }
        public required string UserName { get; init; }
        public required string Name { get; init; }
        public string? Avatar { get; init; }
        public string? Bio { get; init; }

        internal static ChatChannelGetUserReceiverPipelineResponseDto FromUser(User user) => new()
        {
            Id = user.Id,
            UserName = user.UserName,
            Name = user.Name,
            Avatar = user.Avatar,
            Bio = user.Bio
        };
    }
}
