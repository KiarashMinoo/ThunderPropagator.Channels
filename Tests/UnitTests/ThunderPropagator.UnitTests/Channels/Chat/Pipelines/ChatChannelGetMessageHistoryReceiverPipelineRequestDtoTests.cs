using ThunderPropagator.Channels.Chat.Models.Messages;
using ThunderPropagator.Channels.Chat.Pipelines.Messages.History;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    /// <summary>
    /// Issue #118: ChatChannelGetMessageHistoryReceiverPipelineRequestDto.ValidateTarget mirrors
    /// ChatChannelSendMessageReceiverPipelineRequestDto's mutual-exclusivity check — a history
    /// request is either a direct conversation or a group's, never both or neither. Page/PageSize
    /// default to 1/MessageService.DefaultPageSize when the caller omits them.
    /// </summary>
    public sealed class ChatChannelGetMessageHistoryReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void ValidateTarget_WithOnlyReceiverIdSet_DoesNotThrow()
        {
            var dto = new ChatChannelGetMessageHistoryReceiverPipelineRequestDto { ReceiverId = Guid.NewGuid() };

            var exception = Record.Exception(dto.ValidateTarget);

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateTarget_WithOnlyGroupIdSet_DoesNotThrow()
        {
            var dto = new ChatChannelGetMessageHistoryReceiverPipelineRequestDto { GroupId = Guid.NewGuid() };

            var exception = Record.Exception(dto.ValidateTarget);

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateTarget_WithNeitherSet_Throws()
        {
            var dto = new ChatChannelGetMessageHistoryReceiverPipelineRequestDto();

            Assert.Throws<InvalidOperationException>(dto.ValidateTarget);
        }

        [Fact]
        public void ValidateTarget_WithBothSet_Throws()
        {
            var dto = new ChatChannelGetMessageHistoryReceiverPipelineRequestDto
            {
                ReceiverId = Guid.NewGuid(),
                GroupId = Guid.NewGuid()
            };

            Assert.Throws<InvalidOperationException>(dto.ValidateTarget);
        }

        [Fact]
        public void ValidateTarget_WithBothSetToEmptyGuid_Throws()
        {
            var dto = new ChatChannelGetMessageHistoryReceiverPipelineRequestDto
            {
                ReceiverId = Guid.Empty,
                GroupId = Guid.Empty
            };

            Assert.Throws<InvalidOperationException>(dto.ValidateTarget);
        }

        [Fact]
        public void Page_WhenNotSet_DefaultsToOne()
        {
            var dto = new ChatChannelGetMessageHistoryReceiverPipelineRequestDto();

            Assert.Equal(1, dto.Page);
        }

        [Fact]
        public void PageSize_WhenNotSet_DefaultsToMessageServiceDefault()
        {
            var dto = new ChatChannelGetMessageHistoryReceiverPipelineRequestDto();

            Assert.Equal(MessageService.DefaultPageSize, dto.PageSize);
        }

        [Fact]
        public void Page_WhenSet_ReturnsTheAssignedValue()
        {
            var dto = new ChatChannelGetMessageHistoryReceiverPipelineRequestDto { Page = 3 };

            Assert.Equal(3, dto.Page);
        }

        [Fact]
        public void PageSize_WhenSet_ReturnsTheAssignedValue()
        {
            var dto = new ChatChannelGetMessageHistoryReceiverPipelineRequestDto { PageSize = 25 };

            Assert.Equal(25, dto.PageSize);
        }
    }
}
