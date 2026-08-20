using System.Net;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Messages
{
    /// <summary>
    /// Issue #119: thrown by MessageService.DeleteMessageAsync when the caller isn't the message's
    /// sender — the AC's "only the sender can delete the message" / "unauthorized ... return safe
    /// responses".
    /// </summary>
    public class MessageDeleteForbiddenExceptionTests
    {
        [Fact]
        public void MessageDeleteForbiddenException_IsPublic()
        {
            var type = typeof(MessageDeleteForbiddenException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void MessageDeleteForbiddenException_InheritsFromHttpRequestException()
        {
            var type = typeof(MessageDeleteForbiddenException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void MessageDeleteForbiddenException_HasForbiddenStatusCode()
        {
            var exception = new MessageDeleteForbiddenException();

            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        }

        [Fact]
        public void MessageDeleteForbiddenException_CanBeThrown()
        {
            MessageDeleteForbiddenException? exception = null;

            try
            {
                throw new MessageDeleteForbiddenException();
            }
            catch (MessageDeleteForbiddenException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
