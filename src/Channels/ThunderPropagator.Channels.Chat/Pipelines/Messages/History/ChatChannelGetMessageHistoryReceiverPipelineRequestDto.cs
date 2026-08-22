using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.History
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetMessageHistoryReceiverPipelineRequestDto : BindingDictionary<string, object?>, IRequestContentFormCollection
    {
        public Guid? ReceiverId
        {
            get => (Guid?)this[nameof(ReceiverId)];
            set => this[nameof(ReceiverId)] = value;
        }

        public Guid? GroupId
        {
            get => (Guid?)this[nameof(GroupId)];
            set => this[nameof(GroupId)] = value;
        }

        public int Page
        {
            get => (int)GetValueOrDefault(nameof(Page), 1)!;
            set => this[nameof(Page)] = value;
        }

        // Issue #141: null when the caller doesn't specify one, rather than defaulting here to the
        // old hardcoded MessageService.DefaultPageSize constant — MessageService itself now resolves
        // an unset PageSize against the configurable ChatChannelConfiguration.MessageHistoryPageSize.
        public int? PageSize
        {
            get => (int?)this[nameof(PageSize)];
            set => this[nameof(PageSize)] = value;
        }

        /// <summary>
        /// Enforces that exactly one of <see cref="ReceiverId"/>/<see cref="GroupId"/> is set,
        /// mirroring <c>ChatChannelSendMessageReceiverPipelineRequestDto.ValidateTarget</c> — a
        /// history request is either a direct conversation or a group's, never both or neither.
        /// </summary>
        internal void ValidateTarget()
        {
            var hasReceiver = ReceiverId is not null && ReceiverId != Guid.Empty;
            var hasGroup = GroupId is not null && GroupId != Guid.Empty;

            if (hasReceiver == hasGroup)
                throw new InvalidOperationException("Exactly one of ReceiverId or GroupId must be specified.");
        }
    }
}
