using ThunderPropagator.Channels.Chat.Pipelines.Messages.Delete;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    public sealed class ChatChannelDeleteMessageReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void MessageId_Getter_ReturnsTheValueAssignedToMessageId()
        {
            var messageId = Guid.NewGuid();
            var dto = new ChatChannelDeleteMessageReceiverPipelineRequestDto { MessageId = messageId };

            Assert.Equal(messageId, dto.MessageId);
        }

        [Fact]
        public void MessageId_IsStoredUnderItsOwnDictionaryKey()
        {
            // This is the level GetRequestContentFormData<T> actually binds through — the DTO is
            // itself a dictionary, so proving the key round-trips here is the direct equivalent of
            // proving a bound (deserialized) request does too.
            var messageId = Guid.NewGuid();
            var dto = new ChatChannelDeleteMessageReceiverPipelineRequestDto { MessageId = messageId };

            Assert.Equal(messageId, (Guid?)dto[nameof(ChatChannelDeleteMessageReceiverPipelineRequestDto.MessageId)]);
        }
    }
}
