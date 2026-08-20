using ThunderPropagator.Channels.Chat.Pipelines.Groups.Delete;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    public sealed class ChatChannelDeleteGroupReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void GroupId_Getter_ReturnsTheValueAssignedToGroupId()
        {
            var groupId = Guid.NewGuid();
            var dto = new ChatChannelDeleteGroupReceiverPipelineRequestDto { GroupId = groupId };

            Assert.Equal(groupId, dto.GroupId);
        }

        [Fact]
        public void GroupId_IsStoredUnderItsOwnDictionaryKey()
        {
            var groupId = Guid.NewGuid();
            var dto = new ChatChannelDeleteGroupReceiverPipelineRequestDto { GroupId = groupId };

            Assert.Equal(groupId, (Guid?)dto[nameof(ChatChannelDeleteGroupReceiverPipelineRequestDto.GroupId)]);
        }
    }
}
