using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.Clock
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