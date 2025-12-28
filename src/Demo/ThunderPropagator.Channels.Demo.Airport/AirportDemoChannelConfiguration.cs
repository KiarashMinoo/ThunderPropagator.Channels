using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.Airport
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