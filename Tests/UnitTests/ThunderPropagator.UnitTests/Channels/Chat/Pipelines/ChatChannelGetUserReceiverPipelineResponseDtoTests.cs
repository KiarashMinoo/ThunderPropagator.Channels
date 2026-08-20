using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    /// <summary>
    /// Issue #122: the AC's "sensitive credential/session data is never returned" and "projection
    /// ... tests cover the pipeline" — unlike ChatChannelUpdateUserReceiverPipelineResponseDto/the
    /// Login response (both a caller's own profile), this pipeline can return any user's profile, so
    /// it exposes a deliberately reduced type rather than the User entity itself. These tests assert
    /// the exclusion at the type level (no PasswordHash/BirthDate property exists on this DTO at
    /// all), which holds regardless of whether some future serializer respects User's own
    /// [JsonIgnore] on PasswordHash.
    /// </summary>
    public sealed class ChatChannelGetUserReceiverPipelineResponseDtoTests
    {
        private static User CreateUser()
        {
            var user = User.Create("alice", "Alice");
            user.SetPasswordHash("hashed-password");
            user.SetAvatar("avatar.png");
            user.SetBio("Hello, I'm Alice.");
            user.SetBirthDate(new DateOnly(1990, 1, 1));
            return user;
        }

        [Fact]
        public void FromUser_MapsThePublicProfileFields()
        {
            var user = CreateUser();

            var dto = ChatChannelGetUserReceiverPipelineResponseDto.FromUser(user);

            Assert.Equal(user.Id, dto.Id);
            Assert.Equal(user.UserName, dto.UserName);
            Assert.Equal(user.Name, dto.Name);
            Assert.Equal(user.Avatar, dto.Avatar);
            Assert.Equal(user.Bio, dto.Bio);
        }

        [Fact]
        public void ResponseDto_HasNoPasswordHashProperty()
        {
            var type = typeof(ChatChannelGetUserReceiverPipelineResponseDto);

            Assert.Null(type.GetProperty(nameof(User.PasswordHash)));
        }

        [Fact]
        public void ResponseDto_HasNoBirthDateProperty()
        {
            var type = typeof(ChatChannelGetUserReceiverPipelineResponseDto);

            Assert.Null(type.GetProperty(nameof(User.BirthDate)));
        }

        [Fact]
        public void ResponseDto_ExposesOnlyThePublicProfileFields()
        {
            // An exhaustive allow-list, so a future field added to User doesn't silently become part
            // of the public projection just by being added to the mapping method — this test forces
            // a deliberate update here too.
            var type = typeof(ChatChannelGetUserReceiverPipelineResponseDto);

            var propertyNames = type.GetProperties()
                .Where(property => property.DeclaringType == type)
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                new[] { "Avatar", "Bio", "Id", "Name", "UserName" }.OrderBy(name => name, StringComparer.Ordinal),
                propertyNames);
        }
    }
}
