using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.SetAvatar
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUserSetAvatarReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required string Avatar
        {
            get => (string)this[nameof(Avatar)];
            set => this[nameof(Avatar)] = value;
        }
    }
}