using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Demo.Airport;

public
#if !DEBUG
    sealed
#endif
    class AirportDemoChannelFeederConfiguration : AbstractFeederConfiguration
{
    public AirportDemoChannelFeederConfiguration()
    {
        IsEnabled = true;
    }

    internal void Bind(AirportDemoChannelFeederConfiguration airportDemoChannelFeederConfiguration) => base.Bind(airportDemoChannelFeederConfiguration);
}