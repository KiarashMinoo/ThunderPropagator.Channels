using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;

namespace RapidStreamer.Channels.Chat.Pipelines.Users.SetAvatar
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