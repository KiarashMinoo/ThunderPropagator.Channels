using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Online;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    public sealed class ChatChannelGetOnlineUsersReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void Page_WhenNotSet_DefaultsToOne()
        {
            var dto = new ChatChannelGetOnlineUsersReceiverPipelineRequestDto();

            Assert.Equal(1, dto.Page);
        }

        [Fact]
        public void PageSize_WhenNotSet_DefaultsToUserServiceDefault()
        {
            var dto = new ChatChannelGetOnlineUsersReceiverPipelineRequestDto();

            Assert.Equal(UserService.DefaultPageSize, dto.PageSize);
        }

        [Fact]
        public void PageAndPageSize_WhenSet_ReturnTheAssignedValues()
        {
            var dto = new ChatChannelGetOnlineUsersReceiverPipelineRequestDto { Page = 3, PageSize = 25 };

            Assert.Equal(3, dto.Page);
            Assert.Equal(25, dto.PageSize);
        }
    }
}
