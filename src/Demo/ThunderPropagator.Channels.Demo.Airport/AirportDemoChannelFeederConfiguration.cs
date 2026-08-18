using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.Demo.Airport;

public
#if !DEBUG
    sealed
#endif
    class AirportDemoChannelFeederConfiguration : AbstractFeederConfiguration
{
    /// <summary>
    /// How often the feeder polls for flight-board changes. Default: 1 minute.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(1);

    public AirportDemoChannelFeederConfiguration()
    {
        IsEnabled = true;
    }

    internal void Bind(AirportDemoChannelFeederConfiguration airportDemoChannelFeederConfiguration) => base.Bind(airportDemoChannelFeederConfiguration);
}