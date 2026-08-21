using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.Channels.Chat.Pipelines.Users.Online
{
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetOnlineUsersReceiverPipelineRequestDto : BindingDictionary<string, object?>, IRequestContentFormCollection
    {
        public int Page
        {
            get => (int)GetValueOrDefault(nameof(Page), 1)!;
            set => this[nameof(Page)] = value;
        }

        public int PageSize
        {
            get => (int)GetValueOrDefault(nameof(PageSize), UserService.DefaultPageSize)!;
            set => this[nameof(PageSize)] = value;
        }
    }
}
