using ThunderPropagator.Channels.Games.TicTacToe.Game.Players;

namespace ThunderPropagator.Channels.Games.TicTacToe.Game
{
    internal
#if !DEBUG
        sealed
#endif
        class BoardChangedEventArgs
    {
        public Player Player { get; }
        public int Row { get; }
        public int Column { get; }

        public BoardChangedEventArgs(Player player, int row, int column)
        {
            Player = player;
            Row = row;
            Column = column;
        }
    }
}