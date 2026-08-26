using System.Net;
using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Messages
{
    /// <summary>
    /// Issue #120: thrown by MessageService.EditMessageAsync once
    /// ChatChannelConfiguration.MessageEditWindow has elapsed since the message was sent — the AC's
    /// "expired ... edits are rejected".
    /// </summary>
    public class MessageEditWindowExpiredExceptionTests
    {
        [Fact]
        public void MessageEditWindowExpiredException_IsPublic()
        {
            var type = typeof(MessageEditWindowExpiredException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void MessageEditWindowExpiredException_InheritsFromHttpRequestException()
        {
            var type = typeof(MessageEditWindowExpiredException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void MessageEditWindowExpiredException_HasForbiddenStatusCode()
        {
            var exception = new MessageEditWindowExpiredException();

            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        }

        [Fact]
        public void MessageEditWindowExpiredException_CanBeThrown()
        {
            MessageEditWindowExpiredException? exception = null;

            try
            {
                throw new MessageEditWindowExpiredException();
            }
            catch (MessageEditWindowExpiredException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
