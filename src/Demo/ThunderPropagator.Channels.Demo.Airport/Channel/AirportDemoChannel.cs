using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Demo.Airport.Configuration;
using ThunderPropagator.Channels.Demo.Airport.Metadata;

namespace ThunderPropagator.Channels.Demo.Airport.Channel
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