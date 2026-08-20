using System.Net;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Messages
{
    /// <summary>
    /// Issue #118: thrown by MessageService.GetDirectMessageHistoryAsync/GetGroupMessageHistoryAsync
    /// when Page/PageSize are out of bounds — the AC's "invalid page inputs return validation
    /// responses". Mirrors GroupNotFoundExceptionTests' coverage shape.
    /// </summary>
    public class InvalidMessageHistoryPageRequestExceptionTests
    {
        [Fact]
        public void InvalidMessageHistoryPageRequestException_IsPublic()
        {
            var type = typeof(InvalidMessageHistoryPageRequestException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void InvalidMessageHistoryPageRequestException_InheritsFromHttpRequestException()
        {
            var type = typeof(InvalidMessageHistoryPageRequestException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void InvalidMessageHistoryPageRequestException_HasBadRequestStatusCode()
        {
            var exception = new InvalidMessageHistoryPageRequestException("Page must be 1 or greater.");

            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Fact]
        public void InvalidMessageHistoryPageRequestException_CarriesTheGivenMessage()
        {
            var exception = new InvalidMessageHistoryPageRequestException("PageSize must be between 1 and 100.");

            Assert.Equal("PageSize must be between 1 and 100.", exception.Message);
        }

        [Fact]
        public void InvalidMessageHistoryPageRequestException_CanBeThrown()
        {
            InvalidMessageHistoryPageRequestException? exception = null;

            try
            {
                throw new InvalidMessageHistoryPageRequestException("Page must be 1 or greater.");
            }
            catch (InvalidMessageHistoryPageRequestException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
