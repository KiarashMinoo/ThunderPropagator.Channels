using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Demo.StockListBasic
{
    public
#if !DEBUG
        sealed
#endif
        class StockListBasicDemoChannel : AbstractChannel<StockListBasicDemoChannelMetadata>
    {
        public StockListBasicDemoChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}