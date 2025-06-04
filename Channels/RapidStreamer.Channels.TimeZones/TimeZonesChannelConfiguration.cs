using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.TimeZones
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