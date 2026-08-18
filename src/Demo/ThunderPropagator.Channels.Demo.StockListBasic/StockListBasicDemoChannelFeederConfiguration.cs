using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.Demo.StockListBasic;

public
#if !DEBUG
    sealed
#endif
    class StockListBasicDemoChannelFeederConfiguration : AbstractFeederConfiguration
{
    /// <summary>
    /// Lower bound of the randomized poll interval between simulated price updates. Default: 500ms.
    /// </summary>
    public TimeSpan MinPollInterval
    {
        get => Get(TimeSpan.FromMilliseconds(500));
        set => Set(value);
    }

    /// <summary>
    /// Upper bound of the randomized poll interval between simulated price updates. Default: 90s.
    /// </summary>
    public TimeSpan MaxPollInterval
    {
        get => Get(TimeSpan.FromSeconds(90));
        set => Set(value);
    }

    public StockListBasicDemoChannelFeederConfiguration()
    {
        IsEnabled = true;
    }

    internal void Bind(StockListBasicDemoChannelFeederConfiguration stockListBasicDemoChannelFeederConfiguration) => base.Bind(stockListBasicDemoChannelFeederConfiguration);
}