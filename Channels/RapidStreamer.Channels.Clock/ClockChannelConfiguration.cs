using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Clock
{
    public
#if !DEBUG
        sealed
#endif
        class ClockChannelConfiguration : AbstractChannelConfiguration
    {
        public ClockChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}