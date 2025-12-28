using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.StockListBasic
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