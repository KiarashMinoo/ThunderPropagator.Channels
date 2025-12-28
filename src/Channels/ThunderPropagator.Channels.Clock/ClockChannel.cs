using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Clock
{
    public
#if !DEBUG
        sealed
#endif
        class ClockChannel : AbstractChannel<ClockChannelMetadata, ClockChannelConfiguration>
    {
        public ClockChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}