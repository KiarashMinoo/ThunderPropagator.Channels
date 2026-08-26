using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Demo.Airport.Feeders;

namespace ThunderPropagator.Channels.Demo.Airport.Configuration
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