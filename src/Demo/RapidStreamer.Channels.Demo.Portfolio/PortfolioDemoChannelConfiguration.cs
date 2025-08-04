using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Demo.Portfolio
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