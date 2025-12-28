using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Games.TicTacToe
{
    public
#if !DEBUG
        sealed
#endif
        class TicTacToeChannelConfiguration : AbstractChannelConfiguration
    {
        public TicTacToeChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}