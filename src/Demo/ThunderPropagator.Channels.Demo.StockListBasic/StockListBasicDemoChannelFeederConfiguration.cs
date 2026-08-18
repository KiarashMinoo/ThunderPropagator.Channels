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
    public TimeSpan MinPollInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Upper bound of the randomized poll interval between simulated price updates. Default: 90s.
    /// </summary>
    public TimeSpan MaxPollInterval { get; set; } = TimeSpan.FromSeconds(90);

    public StockListBasicDemoChannelFeederConfiguration()
    {
        IsEnabled = true;
    }

    internal void Bind(StockListBasicDemoChannelFeederConfiguration stockListBasicDemoChannelFeederConfiguration) => base.Bind(stockListBasicDemoChannelFeederConfiguration);
}