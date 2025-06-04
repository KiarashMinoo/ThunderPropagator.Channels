using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Clock
{
    public
#if !DEBUG
        sealed
#endif
        class NowClockFeederConfiguration : AbstractFeederConfiguration
    {
        public NowClockFeederConfiguration()
        {
            IsEnabled = true;
        }

        internal void Bind(NowClockFeederConfiguration nowClockFeederConfiguration) => base.Bind(nowClockFeederConfiguration);
    }
}