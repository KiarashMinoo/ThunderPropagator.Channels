using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Games.RockPaperScissors
{
    public
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsChannelConfiguration : AbstractChannelConfiguration
    {
        public RockPaperScissorsChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}