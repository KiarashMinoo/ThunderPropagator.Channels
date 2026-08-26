using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Clock.Feeders;

namespace ThunderPropagator.Channels.Clock.Configuration
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