using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.Clock.Feeders
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