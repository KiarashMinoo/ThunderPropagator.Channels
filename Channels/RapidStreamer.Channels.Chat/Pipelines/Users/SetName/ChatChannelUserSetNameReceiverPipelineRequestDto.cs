using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;

namespace RapidStreamer.Channels.Chat.Pipelines.Users.SetName
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUserSetNameReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required string Name
        {
            get => (string)this[nameof(Name)];
            set => this[nameof(Name)] = value;
        }
    }
}