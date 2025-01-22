using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Demo.Airport
{
    public
#if !DEBUG
        sealed
#endif
        class AirportDemoChannel : AbstractChannel<AirportDemoChannelMetadata>
    {
        public AirportDemoChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}