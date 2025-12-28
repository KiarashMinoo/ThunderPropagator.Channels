using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.StockListBasic
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