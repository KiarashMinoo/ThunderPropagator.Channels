using ThunderPropagator.Channels.Chat.Models.Users;
using ThunderPropagator.Channels.Chat.Pipelines.Users.Search;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    public sealed class ChatChannelSearchUsersReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void Term_Getter_ReturnsTheValueAssignedToTerm()
        {
            var dto = new ChatChannelSearchUsersReceiverPipelineRequestDto { Term = "alice" };

            Assert.Equal("alice", dto.Term);
        }

        [Fact]
        public void Page_WhenNotSet_DefaultsToOne()
        {
            var dto = new ChatChannelSearchUsersReceiverPipelineRequestDto { Term = "alice" };

            Assert.Equal(1, dto.Page);
        }

        [Fact]
        public void PageSize_WhenNotSet_DefaultsToUserServiceDefault()
        {
            var dto = new ChatChannelSearchUsersReceiverPipelineRequestDto { Term = "alice" };

            Assert.Equal(UserService.DefaultPageSize, dto.PageSize);
        }

        [Fact]
        public void PageAndPageSize_WhenSet_ReturnTheAssignedValues()
        {
            var dto = new ChatChannelSearchUsersReceiverPipelineRequestDto { Term = "alice", Page = 3, PageSize = 25 };

            Assert.Equal(3, dto.Page);
            Assert.Equal(25, dto.PageSize);
        }
    }
}
