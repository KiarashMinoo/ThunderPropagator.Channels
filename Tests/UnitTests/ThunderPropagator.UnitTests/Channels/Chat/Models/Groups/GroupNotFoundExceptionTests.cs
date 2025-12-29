using System.Net;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Groups
{
    public class GroupNotFoundExceptionTests
    {
        [Fact]
        public void GroupNotFoundException_IsPublic()
        {
            var type = typeof(GroupNotFoundException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void GroupNotFoundException_InheritsFromHttpRequestException()
        {
            var type = typeof(GroupNotFoundException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void GroupNotFoundException_HasCorrectMessage()
        {
            // Act
            var exception = new GroupNotFoundException();

            // Assert
            Assert.Equal("Group not found", exception.Message);
        }

        [Fact]
        public void GroupNotFoundException_HasNotFoundStatusCode()
        {
            // Act
            var exception = new GroupNotFoundException();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public void GroupNotFoundException_CanBeThrown()
        {
            // Arrange
            GroupNotFoundException? exception = null;

            // Act
            try
            {
                throw new GroupNotFoundException();
            }
            catch (GroupNotFoundException ex)
            {
                exception = ex;
            }

            // Assert
            Assert.NotNull(exception);
        }
    }
}
