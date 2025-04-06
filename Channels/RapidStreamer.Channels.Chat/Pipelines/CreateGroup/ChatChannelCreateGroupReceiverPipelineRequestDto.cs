using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;

namespace RapidStreamer.Channels.Chat.Pipelines.CreateGroup
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelCreateGroupReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required string Name
        {
            get => (string)this[nameof(Name)];
            set => this[nameof(Name)] = value;
        }

        public required Guid[] Users
        {
            get => (Guid[])this[nameof(Users)];
            set => this[nameof(Users)] = value;
        }
    }
}