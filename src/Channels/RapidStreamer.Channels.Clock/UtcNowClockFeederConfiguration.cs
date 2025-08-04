using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Clock
{
    public
#if !DEBUG
        sealed
#endif
        class UtcNowClockFeederConfiguration : AbstractFeederConfiguration
    {
        public UtcNowClockFeederConfiguration()
        {
            IsEnabled = true;
        }

        internal void Bind(UtcNowClockFeederConfiguration utcNowClockFeederConfiguration) => base.Bind(utcNowClockFeederConfiguration);
    }
}