using ThunderPropagator.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.MarkRead
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelMarkMessageReadReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required IReadOnlyCollection<Guid> MarkedRead { get; init; }
        public required IReadOnlyCollection<Guid> FailedMessageIds { get; init; }
    }
}
