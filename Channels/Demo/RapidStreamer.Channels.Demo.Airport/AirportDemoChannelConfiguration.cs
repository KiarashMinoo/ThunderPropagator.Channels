using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Demo.Airport
{
    public
#if !DEBUG
        sealed
#endif
        class AirportDemoChannelConfiguration : AbstractChannelConfiguration
    {
        public AirportDemoChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}