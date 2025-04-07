using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;

namespace RapidStreamer.Channels.Chat.Pipelines.Users.Login
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelLoginReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required string UserName
        {
            get => (string)this[nameof(UserName)];
            set => this[nameof(UserName)] = value;
        }

        public required string Password
        {
            get => (string)this[nameof(Password)];
            set => this[nameof(Password)] = value;
        }
    }
}