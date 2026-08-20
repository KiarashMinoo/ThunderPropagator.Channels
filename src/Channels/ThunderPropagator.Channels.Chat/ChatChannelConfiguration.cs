using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Chat
{
    public
#if !DEBUG
        sealed
#endif
        class ChatChannelConfiguration : AbstractChannelConfiguration
    {
        public ChatChannelConfiguration()
        {
            IsEnabled = true;
        }

        // Issue #120: how long after a message is sent its sender may still edit it —
        // MessageService.EditMessageAsync rejects an edit request once this window has elapsed.
        // 15 minutes is a common chat-app norm; a consumer can widen or narrow it via the
        // channelConfigurator callback AddChatChannel already accepts.
        public TimeSpan MessageEditWindow { get; set; } = TimeSpan.FromMinutes(15);

        // Issue #125: whether marking a message read notifies its sender at all — a single
        // channel-wide switch, same pattern as MessageEditWindow above; a consumer can turn it off
        // via the channelConfigurator callback AddChatChannel already accepts.
        public bool ReadReceiptsEnabled { get; set; } = true;
    }
}