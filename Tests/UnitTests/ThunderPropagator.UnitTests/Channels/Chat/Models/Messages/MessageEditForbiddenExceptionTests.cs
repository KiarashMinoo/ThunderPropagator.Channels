using System.Net;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Messages
{
    /// <summary>
    /// Issue #120: thrown by MessageService.EditMessageAsync when the caller isn't the message's
    /// sender — the AC's "only the sender can edit within the allowed window" / "unauthorized ...
    /// edits are rejected".
    /// </summary>
    public class MessageEditForbiddenExceptionTests
    {
        [Fact]
        public void MessageEditForbiddenException_IsPublic()
        {
            var type = typeof(MessageEditForbiddenException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void MessageEditForbiddenException_InheritsFromHttpRequestException()
        {
            var type = typeof(MessageEditForbiddenException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void MessageEditForbiddenException_HasForbiddenStatusCode()
        {
            var exception = new MessageEditForbiddenException();

            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        }

        [Fact]
        public void MessageEditForbiddenException_CanBeThrown()
        {
            MessageEditForbiddenException? exception = null;

            try
            {
                throw new MessageEditForbiddenException();
            }
            catch (MessageEditForbiddenException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
