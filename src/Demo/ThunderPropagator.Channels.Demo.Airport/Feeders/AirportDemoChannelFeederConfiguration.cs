using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.Demo.Airport.Feeders;

public
#if !DEBUG
    sealed
#endif
    class AirportDemoChannelFeederConfiguration : AbstractFeederConfiguration
{
    /// <summary>
    /// How often the feeder polls for flight-board changes. Default: 1 minute.
    /// </summary>
    public TimeSpan PollInterval
    {
        get => Get(TimeSpan.FromMinutes(1));
        set => Set(value);
    }

    public AirportDemoChannelFeederConfiguration()
    {
        IsEnabled = true;
    }

    internal void Bind(AirportDemoChannelFeederConfiguration airportDemoChannelFeederConfiguration) => base.Bind(airportDemoChannelFeederConfiguration);
}