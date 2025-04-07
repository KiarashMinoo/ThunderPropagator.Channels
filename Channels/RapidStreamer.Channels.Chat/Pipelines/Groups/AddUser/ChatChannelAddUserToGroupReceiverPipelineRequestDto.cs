using RapidStreamer.Application.Collections;
using RapidStreamer.BuildingBlocks.Application.Collections;

namespace RapidStreamer.Channels.Chat.Pipelines.Groups.AddUser
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelAddUserToGroupReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required Guid GroupId
        {
            get => (Guid)this[nameof(GroupId)];
            set => this[nameof(GroupId)] = value;
        }

        public required Guid UserId
        {
            get => (Guid)this[nameof(UserId)];
            set => this[nameof(UserId)] = value;
        }
    }
}