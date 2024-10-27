using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Clock
{
    internal
#if !DEBUG
        sealed
#endif
        class UtcNowClockFeederConfiguration : AbstractFeederConfiguration;
}