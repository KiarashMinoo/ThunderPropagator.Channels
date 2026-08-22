using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Models.Messages;

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
    }
}
