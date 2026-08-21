using System.Net;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Users
{
    /// <summary>
    /// Issue #126: thrown by UserService.GetOnlineContactsAsync for an out-of-bounds paging request.
    /// Mirrors InvalidUserSearchRequestExceptionTests' coverage shape.
    /// </summary>
    public class InvalidOnlineUsersRequestExceptionTests
    {
        [Fact]
        public void InvalidOnlineUsersRequestException_IsPublic()
        {
            var type = typeof(InvalidOnlineUsersRequestException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void InvalidOnlineUsersRequestException_InheritsFromHttpRequestException()
        {
            var type = typeof(InvalidOnlineUsersRequestException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void InvalidOnlineUsersRequestException_HasBadRequestStatusCode()
        {
            var exception = new InvalidOnlineUsersRequestException("Page must be 1 or greater.");

            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Fact]
        public void InvalidOnlineUsersRequestException_CarriesTheGivenMessage()
        {
            var exception = new InvalidOnlineUsersRequestException("Page must be 1 or greater.");

            Assert.Equal("Page must be 1 or greater.", exception.Message);
        }

        [Fact]
        public void InvalidOnlineUsersRequestException_CanBeThrown()
        {
            InvalidOnlineUsersRequestException? exception = null;

            try
            {
                throw new InvalidOnlineUsersRequestException("Page must be 1 or greater.");
            }
            catch (InvalidOnlineUsersRequestException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
