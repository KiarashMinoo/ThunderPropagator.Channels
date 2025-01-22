using RapidStreamer.Application.Feeders;

namespace RapidStreamer.Channels.Demo.StockListBasic;

internal
#if !DEBUG
        sealed
#endif
    class StockListBasicDemoChannelFeederConfiguration : AbstractFeederConfiguration;