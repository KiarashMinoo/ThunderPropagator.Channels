using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Exceptions;

namespace ThunderPropagator.Channels.Games.TicTacToe.Game.Players
{
    internal
#if !DEBUG
        sealed
#endif
        class HumanPlayer : Player
    {
        public override PlayerKind Kind => PlayerKind.Human;

        public HumanPlayer(string name, PlayerSign sign, string connectionId) : base(name, sign, connectionId)
        {
        }

        public void HumanMove(int row, int column)
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);

            if (!TicTacToeGame.IsValidMove(row, column))
                throw new InvalidMoveException();

            OnBeforePlayerMovedHandler();
            TicTacToeGame.Move(this, row, column);
            OnPlayerMoved();
        }
    }
}