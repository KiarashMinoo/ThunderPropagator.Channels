using ThunderPropagator.Channels.Chat.Pipelines.Messages.MarkRead;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    public sealed class ChatChannelMarkMessageReadReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void MessageIds_Getter_ReturnsTheValueAssignedToMessageIds()
        {
            var messageIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var dto = new ChatChannelMarkMessageReadReceiverPipelineRequestDto { MessageIds = messageIds };

            Assert.Equal(messageIds, dto.MessageIds);
        }

        [Fact]
        public void MessageIds_IsStoredUnderItsOwnDictionaryKey()
        {
            // This is the level GetRequestContentFormData<T> actually binds through — the DTO is
            // itself a dictionary, so proving the key round-trips here is the direct equivalent of
            // proving a bound (deserialized) request does too.
            var messageIds = new[] { Guid.NewGuid() };
            var dto = new ChatChannelMarkMessageReadReceiverPipelineRequestDto { MessageIds = messageIds };

            Assert.Equal(messageIds, (Guid[])dto[nameof(ChatChannelMarkMessageReadReceiverPipelineRequestDto.MessageIds)]);
        }
    }
}
