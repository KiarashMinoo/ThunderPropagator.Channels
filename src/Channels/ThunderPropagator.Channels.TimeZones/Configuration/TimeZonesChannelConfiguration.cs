using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.TimeZones.Feeders;

namespace ThunderPropagator.Channels.TimeZones.Configuration
{
    public
#if !DEBUG
        sealed
#endif
        class TimeZonesChannelConfiguration : AbstractChannelConfiguration
    {
        public TimeZonesChannelFeederConfiguration FeederConfiguration { get; set; } = new();

        public TimeZonesChannelConfiguration()
        {
            IsEnabled = false;
        }
    }
}