using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Register
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelRegisterReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
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

        public required string Name
        {
            get => (string)this[nameof(Name)];
            set => this[nameof(Name)] = value;
        }
    }
}