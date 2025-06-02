using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.Games.TicTacToe
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