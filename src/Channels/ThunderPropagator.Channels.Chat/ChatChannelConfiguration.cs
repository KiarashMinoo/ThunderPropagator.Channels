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
    }
}