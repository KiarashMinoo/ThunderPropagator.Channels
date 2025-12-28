using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Update
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelUpdateUserReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        public required string Bio
        {
            get => (string)this[nameof(Bio)];
            set => this[nameof(Bio)] = value;
        }

        public required DateOnly BirthDate
        {
            get => (DateOnly)this[nameof(BirthDate)];
            set => this[nameof(BirthDate)] = value;
        }
    }
}