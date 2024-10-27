using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Clock
{
    public
#if !DEBUG
        sealed
#endif
        class ClockChannel : AbstractChannel<ClockChannelMetadata>
    {
        public ClockChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}