using ThunderPropagator.Channels.Chat.Pipelines.Users.Get;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    public sealed class ChatChannelGetUserReceiverPipelineRequestDtoTests
    {
        [Fact]
        public void UserId_Getter_ReturnsTheValueAssignedToUserId()
        {
            var userId = Guid.NewGuid();
            var dto = new ChatChannelGetUserReceiverPipelineRequestDto { UserId = userId };

            Assert.Equal(userId, dto.UserId);
        }

        [Fact]
        public void UserId_IsStoredUnderItsOwnDictionaryKey()
        {
            // This is the level GetRequestContentFormData<T> actually binds through — the DTO is
            // itself a dictionary, so proving the key round-trips here is the direct equivalent of
            // proving a bound (deserialized) request does too.
            var userId = Guid.NewGuid();
            var dto = new ChatChannelGetUserReceiverPipelineRequestDto { UserId = userId };

            Assert.Equal(userId, (Guid?)dto[nameof(ChatChannelGetUserReceiverPipelineRequestDto.UserId)]);
        }
    }
}
