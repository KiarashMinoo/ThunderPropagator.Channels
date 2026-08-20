using System.Net;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Users
{
    /// <summary>
    /// Issue #123: thrown by UserService.SearchUsersAsync for an out-of-bounds search term or
    /// paging request — the AC's "empty, too-short, and oversized terms are validated". Mirrors
    /// InvalidMessageHistoryPageRequestExceptionTests' coverage shape.
    /// </summary>
    public class InvalidUserSearchRequestExceptionTests
    {
        [Fact]
        public void InvalidUserSearchRequestException_IsPublic()
        {
            var type = typeof(InvalidUserSearchRequestException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void InvalidUserSearchRequestException_InheritsFromHttpRequestException()
        {
            var type = typeof(InvalidUserSearchRequestException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void InvalidUserSearchRequestException_HasBadRequestStatusCode()
        {
            var exception = new InvalidUserSearchRequestException("Search term must be at least 2 characters.");

            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Fact]
        public void InvalidUserSearchRequestException_CarriesTheGivenMessage()
        {
            var exception = new InvalidUserSearchRequestException("Search term must be at least 2 characters.");

            Assert.Equal("Search term must be at least 2 characters.", exception.Message);
        }

        [Fact]
        public void InvalidUserSearchRequestException_CanBeThrown()
        {
            InvalidUserSearchRequestException? exception = null;

            try
            {
                throw new InvalidUserSearchRequestException("Search term must be at least 2 characters.");
            }
            catch (InvalidUserSearchRequestException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
