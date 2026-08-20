using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Get
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetUserReceiverPipelineRequestDto : BindingDictionary<string, object?>, IRequestContentFormCollection
    {
        public required Guid UserId
        {
            get => (Guid)GetValueOrDefault(nameof(UserId), Guid.Empty)!;
            set => this[nameof(UserId)] = value;
        }
    }
}
