using ThunderPropagator.Channels.Chat.Pipelines.Messages.Send;

namespace ThunderPropagator.UnitTests.Channels.Chat.Pipelines
{
    /// <summary>
    /// Issue #105: GroupId's getter and setter both read/wrote the ReceiverId dictionary key, so
    /// setting GroupId silently overwrote (and was itself read back from) ReceiverId's storage slot
    /// instead of its own — a group-targeted request either lost its group identifier or corrupted
    /// the direct-message recipient, depending on which property was touched last. GroupId now maps
    /// to its own key. ValidateTarget also now rejects a request that sets both ReceiverId and
    /// GroupId, not just one that sets neither, since specifying both leaves the destination
    /// ambiguous.
    /// </summary>
    public sealed class ChatChannelSendMessageReceiverPipelineRequestDtoTests
    {
        private static ChatChannelSendMessageReceiverPipelineRequestDto CreateDto() => new() { Body = "hello" };

        [Fact]
        public void GroupId_Getter_ReturnsTheValueAssignedToGroupId()
        {
            var groupId = Guid.NewGuid();
            var dto = CreateDto();

            dto.GroupId = groupId;

            Assert.Equal(groupId, dto.GroupId);
        }

        [Fact]
        public void SettingGroupId_DoesNotModifyReceiverId()
        {
            var dto = CreateDto();

            dto.GroupId = Guid.NewGuid();

            Assert.Null(dto.ReceiverId);
        }

        [Fact]
        public void SettingReceiverId_DoesNotModifyGroupId()
        {
            var dto = CreateDto();

            dto.ReceiverId = Guid.NewGuid();

            Assert.Null(dto.GroupId);
        }

        [Fact]
        public void ReceiverIdAndGroupId_AreStoredUnderIndependentDictionaryKeys()
        {
            // This is the level GetRequestContentFormData<T> actually binds through — the DTO is
            // itself a dictionary, so proving the two keys are independent here is the direct
            // equivalent of proving a bound (deserialized) request keeps them independent.
            var receiverId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var dto = CreateDto();

            dto.ReceiverId = receiverId;
            dto.GroupId = groupId;

            Assert.Equal(receiverId, (Guid?)dto[nameof(ChatChannelSendMessageReceiverPipelineRequestDto.ReceiverId)]);
            Assert.Equal(groupId, (Guid?)dto[nameof(ChatChannelSendMessageReceiverPipelineRequestDto.GroupId)]);
        }

        [Fact]
        public void ValidateTarget_WithOnlyReceiverIdSet_DoesNotThrow()
        {
            var dto = CreateDto();
            dto.ReceiverId = Guid.NewGuid();

            var exception = Record.Exception(dto.ValidateTarget);

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateTarget_WithOnlyGroupIdSet_DoesNotThrow()
        {
            var dto = CreateDto();
            dto.GroupId = Guid.NewGuid();

            var exception = Record.Exception(dto.ValidateTarget);

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateTarget_WithNeitherSet_Throws()
        {
            var dto = CreateDto();

            Assert.Throws<InvalidOperationException>(dto.ValidateTarget);
        }

        [Fact]
        public void ValidateTarget_WithBothSet_Throws()
        {
            var dto = CreateDto();
            dto.ReceiverId = Guid.NewGuid();
            dto.GroupId = Guid.NewGuid();

            Assert.Throws<InvalidOperationException>(dto.ValidateTarget);
        }

        [Fact]
        public void ValidateTarget_WithBothSetToEmptyGuid_Throws()
        {
            // Guid.Empty is treated as "unset" by the pipeline's own is-not-null-and-not-empty check
            // (see ChatChannelSendMessageReceiverPipeline) — mirrored here so ValidateTarget agrees.
            var dto = CreateDto();
            dto.ReceiverId = Guid.Empty;
            dto.GroupId = Guid.Empty;

            Assert.Throws<InvalidOperationException>(dto.ValidateTarget);
        }
    }
}
