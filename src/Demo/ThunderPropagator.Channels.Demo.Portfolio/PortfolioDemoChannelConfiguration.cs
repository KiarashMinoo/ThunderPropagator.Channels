using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.Portfolio
{
    public
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelConfiguration : AbstractChannelConfiguration
    {
        public PortfolioDemoChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}