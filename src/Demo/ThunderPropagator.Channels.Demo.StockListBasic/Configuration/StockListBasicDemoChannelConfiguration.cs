using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Demo.StockListBasic.Feeders;

namespace ThunderPropagator.Channels.Demo.StockListBasic.Configuration
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