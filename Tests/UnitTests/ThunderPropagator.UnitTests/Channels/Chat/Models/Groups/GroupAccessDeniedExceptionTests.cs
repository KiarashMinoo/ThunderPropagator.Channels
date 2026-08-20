using System.Net;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models.Groups
{
    /// <summary>
    /// Issue #117/#118: thrown by MessageService.GetGroupMessageHistoryAsync when the caller isn't a
    /// member of the requested group — the AC's "unauthorized conversation access is rejected" for
    /// the group case. Mirrors GroupNotFoundExceptionTests' coverage shape.
    /// </summary>
    public class GroupAccessDeniedExceptionTests
    {
        [Fact]
        public void GroupAccessDeniedException_IsPublic()
        {
            var type = typeof(GroupAccessDeniedException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void GroupAccessDeniedException_InheritsFromHttpRequestException()
        {
            var type = typeof(GroupAccessDeniedException);
            Assert.True(typeof(HttpRequestException).IsAssignableFrom(type));
        }

        [Fact]
        public void GroupAccessDeniedException_HasForbiddenStatusCode()
        {
            var exception = new GroupAccessDeniedException();

            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        }

        [Fact]
        public void GroupAccessDeniedException_CanBeThrown()
        {
            GroupAccessDeniedException? exception = null;

            try
            {
                throw new GroupAccessDeniedException();
            }
            catch (GroupAccessDeniedException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }
    }
}
