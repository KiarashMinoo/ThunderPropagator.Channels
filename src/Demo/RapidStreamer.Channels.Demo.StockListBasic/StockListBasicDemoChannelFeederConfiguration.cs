using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Demo.StockListBasic;

public
#if !DEBUG
    sealed
#endif
    class StockListBasicDemoChannelFeederConfiguration : AbstractFeederConfiguration
{
    public StockListBasicDemoChannelFeederConfiguration()
    {
        IsEnabled = true;
    }

    internal void Bind(StockListBasicDemoChannelFeederConfiguration stockListBasicDemoChannelFeederConfiguration) => base.Bind(stockListBasicDemoChannelFeederConfiguration);
}