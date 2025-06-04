using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Clock
{
    public
#if !DEBUG
        sealed
#endif
        class ClockChannelConfiguration : AbstractChannelConfiguration
    {
        public NowClockFeederConfiguration NowClockFeederConfiguration { get; set; } = new();
        public UtcNowClockFeederConfiguration UtcNowClockFeederConfiguration { get; set; } = new();
        
        public ClockChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}