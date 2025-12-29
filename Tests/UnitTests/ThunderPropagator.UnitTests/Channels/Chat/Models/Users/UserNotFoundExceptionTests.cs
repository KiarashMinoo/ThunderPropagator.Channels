using System.Net;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Users
{
    public class UserNotFoundExceptionTests
    {
        [Fact]
        public void UserNotFoundException_IsPublic()
        {
            var type = typeof(UserNotFoundException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void UserNotFoundException_InheritsFromHttpRequestException()
        {
            var type = typeof(UserNotFoundException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void UserNotFoundException_HasCorrectMessage()
        {
            // Act
            var exception = new UserNotFoundException();

            // Assert
            Assert.Equal("User not found", exception.Message);
        }

        [Fact]
        public void UserNotFoundException_HasNotFoundStatusCode()
        {
            // Act
            var exception = new UserNotFoundException();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public void UserNotFoundException_CanBeThrown()
        {
            // Arrange
            UserNotFoundException? exception = null;

            // Act
            try
            {
                throw new UserNotFoundException();
            }
            catch (UserNotFoundException ex)
            {
                exception = ex;
            }

            // Assert
            Assert.NotNull(exception);
        }
    }
}
