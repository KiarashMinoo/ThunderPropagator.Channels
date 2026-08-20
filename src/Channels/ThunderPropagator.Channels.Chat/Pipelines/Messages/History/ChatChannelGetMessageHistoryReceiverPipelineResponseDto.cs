using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.Channels.Chat.Pipelines.Messages.History
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetMessageHistoryReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required IReadOnlyCollection<Message> Messages { get; init; }
        public required int TotalCount { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
    }
}
