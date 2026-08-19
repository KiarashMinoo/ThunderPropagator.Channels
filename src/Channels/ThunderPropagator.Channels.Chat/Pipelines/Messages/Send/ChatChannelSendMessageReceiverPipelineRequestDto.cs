using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.Send
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelSendMessageReceiverPipelineRequestDto : BindingDictionary<string, object?>, IRequestContentFormCollection
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

        public required string Body
        {
            get => (string)GetValueOrDefault(nameof(Body), string.Empty)!;
            set => this[nameof(Body)] = value;
        }

        /// <summary>
        /// Enforces that exactly one of <see cref="ReceiverId"/>/<see cref="GroupId"/> is set (see
        /// #105) — neither leaves the message with no destination, and both leave it ambiguous
        /// whether it's a direct message or a group message. Called by the pipeline immediately
        /// after binding the request, before either target is acted on.
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