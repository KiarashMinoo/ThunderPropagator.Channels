using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Demo.StockListBasic
{
    public
#if !DEBUG
        sealed
#endif
        class StockListBasicDemoChannelConfiguration : AbstractChannelConfiguration
    {
        public StockListBasicDemoChannelFeederConfiguration FeederConfiguration { get; set; } = new();

        public StockListBasicDemoChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}