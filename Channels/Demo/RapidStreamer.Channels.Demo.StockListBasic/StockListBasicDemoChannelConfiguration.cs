using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Demo.StockListBasic
{
    public
#if !DEBUG
        sealed
#endif
        class StockListBasicDemoChannelConfiguration : AbstractChannelConfiguration
    {
        public StockListBasicDemoChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}