using System.Net;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Groups
{
    /// <summary>
    /// Issue #124: thrown by GroupService.DeleteGroupAsync when the caller isn't the group's creator
    /// — this domain's only admin concept.
    /// </summary>
    public class GroupDeleteForbiddenExceptionTests
    {
        [Fact]
        public void GroupDeleteForbiddenException_IsPublic()
        {
            var type = typeof(GroupDeleteForbiddenException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void GroupDeleteForbiddenException_InheritsFromHttpRequestException()
        {
            var type = typeof(GroupDeleteForbiddenException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void GroupDeleteForbiddenException_HasForbiddenStatusCode()
        {
            var exception = new GroupDeleteForbiddenException();

            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        }

        [Fact]
        public void GroupDeleteForbiddenException_CanBeThrown()
        {
            GroupDeleteForbiddenException? exception = null;

            try
            {
                throw new GroupDeleteForbiddenException();
            }
            catch (GroupDeleteForbiddenException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
