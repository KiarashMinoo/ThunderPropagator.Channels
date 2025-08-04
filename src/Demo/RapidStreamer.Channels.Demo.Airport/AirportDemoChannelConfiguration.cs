using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Demo.Airport
{
    public
#if !DEBUG
        sealed
#endif
        class AirportDemoChannelConfiguration : AbstractChannelConfiguration
    {
        public AirportDemoChannelFeederConfiguration FeederConfiguration { get; set; } = new();

        public AirportDemoChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}