using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Demo.Airport;

internal
#if !DEBUG
        sealed
#endif
    class AirportDemoChannelFeederConfiguration : AbstractFeederConfiguration;