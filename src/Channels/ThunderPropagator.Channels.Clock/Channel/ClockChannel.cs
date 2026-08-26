using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Clock.Configuration;
using ThunderPropagator.Channels.Clock.Metadata;

namespace ThunderPropagator.Channels.Clock.Channel
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