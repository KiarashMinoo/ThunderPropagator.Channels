using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.UnitTests.Channels.Chat
{
    /// <summary>
    /// Issue #141: covers ChatChannelConfiguration's chat-specific limits — MaxMessageLength,
    /// MessageHistoryPageSize, and AllowGuestRegister (MaxGroupMembers, the setting this issue's own
    /// "MaxGroupSize" AC item refers to, already exists and is already covered — see #136) — and
    /// their startup-time range validation. Enforcement of the values themselves (in
    /// MessageService/UserService/the REST endpoints) is covered where those flows are already
    /// tested: ChatChannelEndpointsTests and UserServiceAuthenticationTests.
    /// </summary>
    public sealed class ChatChannelConfigurationTests
    {
        [Fact]
        public void Defaults_MatchTheDocumentedValues()
        {
            var configuration = new ChatChannelConfiguration();

            Assert.Equal(4000, configuration.MaxMessageLength);
            Assert.Equal(250, configuration.MaxGroupMembers);
            Assert.Equal(MessageService.DefaultPageSize, configuration.MessageHistoryPageSize);
            Assert.True(configuration.AllowGuestRegister);
            Assert.Equal(100, configuration.MaxGroupNameLength);
            Assert.Equal(50, configuration.MaxUserNameLength);
            Assert.Equal(100, configuration.MaxDisplayNameLength);
            Assert.Equal(500, configuration.MaxBioLength);
            Assert.Equal(2048, configuration.MaxAvatarLength);
            Assert.Equal(2048, configuration.MaxGroupIconLength);
            Assert.Equal(128, configuration.MaxPasswordLength);
        }

        [Fact]
        public void Validate_WithAllDefaultValues_DoesNotThrow()
        {
            var configuration = new ChatChannelConfiguration();

            var exception = Record.Exception(configuration.Validate);

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMaxMessageLength_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MaxMessageLength = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MaxMessageLength), exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMaxGroupMembers_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MaxGroupMembers = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MaxGroupMembers), exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMessageHistoryPageSize_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MessageHistoryPageSize = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MessageHistoryPageSize), exception.ParamName);
        }

        [Fact]
        public void Validate_WithAMessageHistoryPageSizeAboveMaxPageSize_ThrowsForThatProperty()
        {
            var configuration = new ChatChannelConfiguration { MessageHistoryPageSize = MessageService.MaxPageSize + 1 };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MessageHistoryPageSize), exception.ParamName);
        }

        // Issue #38: MaxGroupNameLength/MaxUserNameLength/MaxDisplayNameLength/MaxBioLength/
        // MaxAvatarLength/MaxGroupIconLength/MaxPasswordLength — the length bounds GroupService/
        // UserService enforce for previously-unbounded free-text fields. Each gets the same
        // non-positive-value coverage MaxMessageLength/MaxGroupMembers already have above; enforcement
        // of the values themselves is covered where GroupService/UserService are already tested
        // (GroupServiceValidationTests, UserServiceValidationTests).
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMaxGroupNameLength_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MaxGroupNameLength = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MaxGroupNameLength), exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMaxUserNameLength_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MaxUserNameLength = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MaxUserNameLength), exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMaxDisplayNameLength_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MaxDisplayNameLength = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MaxDisplayNameLength), exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMaxBioLength_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MaxBioLength = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MaxBioLength), exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMaxAvatarLength_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MaxAvatarLength = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MaxAvatarLength), exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMaxGroupIconLength_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MaxGroupIconLength = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MaxGroupIconLength), exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithANonPositiveMaxPasswordLength_ThrowsForThatProperty(int value)
        {
            var configuration = new ChatChannelConfiguration { MaxPasswordLength = value };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(configuration.Validate);

            Assert.Equal(nameof(ChatChannelConfiguration.MaxPasswordLength), exception.ParamName);
        }
    }
}
