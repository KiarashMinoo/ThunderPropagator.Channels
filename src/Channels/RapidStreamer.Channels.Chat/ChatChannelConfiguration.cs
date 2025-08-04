using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Chat
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