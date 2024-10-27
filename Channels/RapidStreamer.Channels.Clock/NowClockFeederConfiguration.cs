using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Clock
{
    internal
#if !DEBUG
        sealed
#endif
        class NowClockFeederConfiguration : AbstractFeederConfiguration;
}