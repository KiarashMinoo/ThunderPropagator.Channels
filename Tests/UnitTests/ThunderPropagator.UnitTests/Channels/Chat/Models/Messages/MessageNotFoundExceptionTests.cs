using System.Net;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Messages
{
    /// <summary>
    /// Issue #119: thrown by MessageService.DeleteMessageAsync for an unknown message id — the AC's
    /// "unauthorized and unknown ids return safe responses" for the missing case.
    /// </summary>
    public class MessageNotFoundExceptionTests
    {
        [Fact]
        public void MessageNotFoundException_IsPublic()
        {
            var type = typeof(MessageNotFoundException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void MessageNotFoundException_InheritsFromHttpRequestException()
        {
            var type = typeof(MessageNotFoundException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void MessageNotFoundException_HasCorrectMessage()
        {
            var exception = new MessageNotFoundException();

            Assert.Equal("Message not found", exception.Message);
        }

        [Fact]
        public void MessageNotFoundException_HasNotFoundStatusCode()
        {
            var exception = new MessageNotFoundException();

            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public void MessageNotFoundException_CanBeThrown()
        {
            MessageNotFoundException? exception = null;

            try
            {
                throw new MessageNotFoundException();
            }
            catch (MessageNotFoundException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
