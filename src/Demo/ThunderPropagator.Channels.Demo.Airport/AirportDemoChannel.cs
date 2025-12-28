using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.Airport
{
    public
#if !DEBUG
        sealed
#endif
        class AirportDemoChannel : AbstractChannel<AirportDemoChannelMetadata, AirportDemoChannelConfiguration>
    {
        public AirportDemoChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}