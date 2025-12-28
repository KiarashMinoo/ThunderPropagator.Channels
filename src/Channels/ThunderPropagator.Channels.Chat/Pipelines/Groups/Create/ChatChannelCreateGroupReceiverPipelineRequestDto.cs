using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.Create
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