using System.Net;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Messages
{
    /// <summary>
    /// Issue #120: thrown by MessageService.EditMessageAsync when the revised body is null/blank —
    /// the AC's "invalid edits are rejected". Mirrors InvalidMessageHistoryPageRequestExceptionTests'
    /// coverage shape.
    /// </summary>
    public class InvalidMessageEditExceptionTests
    {
        [Fact]
        public void InvalidMessageEditException_IsPublic()
        {
            var type = typeof(InvalidMessageEditException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void InvalidMessageEditException_InheritsFromHttpRequestException()
        {
            var type = typeof(InvalidMessageEditException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void InvalidMessageEditException_HasBadRequestStatusCode()
        {
            var exception = new InvalidMessageEditException("Body cannot be empty.");

            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Fact]
        public void InvalidMessageEditException_CarriesTheGivenMessage()
        {
            var exception = new InvalidMessageEditException("Body cannot be empty.");

            Assert.Equal("Body cannot be empty.", exception.Message);
        }

        [Fact]
        public void InvalidMessageEditException_CanBeThrown()
        {
            InvalidMessageEditException? exception = null;

            try
            {
                throw new InvalidMessageEditException("Body cannot be empty.");
            }
            catch (InvalidMessageEditException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
