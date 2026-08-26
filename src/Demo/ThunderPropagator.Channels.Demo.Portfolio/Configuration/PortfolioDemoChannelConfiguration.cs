using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.Portfolio.Configuration
{
    public
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelConfiguration : AbstractChannelConfiguration
    {
        /// <summary>
        /// Lower bound of the randomized poll interval between simulated portfolio price updates.
        /// Default: 500ms.
        /// </summary>
        public TimeSpan MinPollInterval
        {
            get;
            set => SetField(ref field, value);
        } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Upper bound of the randomized poll interval between simulated portfolio price updates.
        /// Default: 90s.
        /// </summary>
        public TimeSpan MaxPollInterval
        {
            get;
            set => SetField(ref field, value);
        } = TimeSpan.FromSeconds(90);

        public PortfolioDemoChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}