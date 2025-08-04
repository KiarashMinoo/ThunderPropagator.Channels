using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Games.RockPaperScissors
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