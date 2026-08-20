using ThunderPropagator.Channels.Chat.Pipelines.Messages.Edit;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    public sealed class ChatChannelEditMessageReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void MessageId_Getter_ReturnsTheValueAssignedToMessageId()
        {
            var messageId = Guid.NewGuid();
            var dto = new ChatChannelEditMessageReceiverPipelineRequestDto { MessageId = messageId, Body = "hello" };

            Assert.Equal(messageId, dto.MessageId);
        }

        [Fact]
        public void Body_Getter_ReturnsTheValueAssignedToBody()
        {
            var dto = new ChatChannelEditMessageReceiverPipelineRequestDto { MessageId = Guid.NewGuid(), Body = "revised" };

            Assert.Equal("revised", dto.Body);
        }

        [Fact]
        public void MessageIdAndBody_AreStoredUnderIndependentDictionaryKeys()
        {
            // This is the level GetRequestContentFormData<T> actually binds through — the DTO is
            // itself a dictionary, so proving the two keys round-trip independently here is the
            // direct equivalent of proving a bound (deserialized) request does too.
            var messageId = Guid.NewGuid();
            var dto = new ChatChannelEditMessageReceiverPipelineRequestDto { MessageId = messageId, Body = "revised" };

            Assert.Equal(messageId, (Guid?)dto[nameof(ChatChannelEditMessageReceiverPipelineRequestDto.MessageId)]);
            Assert.Equal("revised", (string?)dto[nameof(ChatChannelEditMessageReceiverPipelineRequestDto.Body)]);
        }
    }
}
