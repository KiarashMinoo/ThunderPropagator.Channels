using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Demo.StockListBasic.Configuration;
using ThunderPropagator.Channels.Demo.StockListBasic.Metadata;

namespace ThunderPropagator.Channels.Demo.StockListBasic.Channel
{
    public
#if !DEBUG
        sealed
#endif
        class StockListBasicDemoChannel : AbstractChannel<StockListBasicDemoChannelMetadata, StockListBasicDemoChannelConfiguration>
    {
        public StockListBasicDemoChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}