using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;

namespace ThunderPropagator.Channels.Games.TicTacToe.Game.Players
{
    internal
#if !DEBUG
        sealed
#endif
        class ComputerPlayer : Player
    {
        private readonly DifficultyLevel _difficulty;
        private Player? _otherPlayer;

        public override PlayerKind Kind => PlayerKind.Computer;

        public ComputerPlayer(PlayerSign sign, DifficultyLevel difficulty) : base($"{nameof(ThunderPropagator)} Computer", sign, null)
        {
            _difficulty = difficulty;
        }

        internal override void SetTicTacToeGame(TicTacToeGame ticTacToeGame)
        {
            base.SetTicTacToeGame(ticTacToeGame);
            _otherPlayer = ticTacToeGame.Player1 == this ? ticTacToeGame.Player2 : ticTacToeGame.Player1;
        }

        private void RandomMove()
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);

            var rand = new Random();
            int row, column;
            do
            {
                row = rand.Next(0, 3);
                column = rand.Next(0, 3);
            } while (!TicTacToeGame.IsValidMove(row, column));

            TicTacToeGame.Move(this, row, column);
        }

        private bool BlockOrWin()
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);
            ArgumentNullException.ThrowIfNull(_otherPlayer);

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    if (TicTacToeGame.CellIsEmpty(row, column))
                    {
                        TicTacToeGame.Move(this, row, column);
                        if (TicTacToeGame.CheckWinner(this))
                            return true;

                        TicTacToeGame.Move(_otherPlayer, row, column);
                        if (TicTacToeGame.CheckWinner(_otherPlayer))
                        {
                            TicTacToeGame.Move(this, row, column);
                            return true;
                        }

                        TicTacToeGame.EmptyCell(row, column);
                    }
                }
            }

            return false;
        }

        private void MinimaxMove()
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);

            int bestScore = int.MinValue;
            int moveRow = -1, moveColumn = -1;

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    if (TicTacToeGame.CellIsEmpty(row, column))
                    {
                        TicTacToeGame.Move(this, row, column);
                        int score = Minimax(false);
                        TicTacToeGame.EmptyCell(row, column);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            moveRow = row;
                            moveColumn = column;
                        }
                    }
                }
            }

            TicTacToeGame.Move(this, moveRow, moveColumn);
        }

        private int Minimax(bool isMaximizing)
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);
            ArgumentNullException.ThrowIfNull(_otherPlayer);

            if (TicTacToeGame.CheckWinner(this)) return 1;
            if (TicTacToeGame.CheckWinner(_otherPlayer)) return -1;
            if (TicTacToeGame.IsBoardFull()) return 0;

            int bestScore = isMaximizing ? int.MinValue : int.MaxValue;

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    if (TicTacToeGame.CellIsEmpty(row, column))
                    {
                        TicTacToeGame.Move(isMaximizing ? this : _otherPlayer, row, column);
                        var score = Minimax(!isMaximizing);
                        TicTacToeGame.EmptyCell(row, column);
                        bestScore = isMaximizing ? Math.Max(score, bestScore) : Math.Min(score, bestScore);
                    }
                }
            }

            return bestScore;
        }

        public void ComputerMove()
        {
            OnBeforePlayerMovedHandler();

            switch (_difficulty)
            {
                case DifficultyLevel.Easy:
                    RandomMove();
                    break;
                case DifficultyLevel.Medium:
                {
                    if (!BlockOrWin())
                    {
                        RandomMove();
                    }

                    break;
                }
                case DifficultyLevel.Hard:
                    MinimaxMove();
                    break;
            }

            OnPlayerMoved();
        }
    }
}