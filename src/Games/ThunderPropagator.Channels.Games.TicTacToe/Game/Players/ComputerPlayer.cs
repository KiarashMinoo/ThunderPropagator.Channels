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

        private (int Row, int Column) ChooseRandomCell()
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);

            int row, column;
            do
            {
                row = Random.Shared.Next(0, 3);
                column = Random.Shared.Next(0, 3);
            } while (!TicTacToeGame.IsValidMove(row, column));

            return (row, column);
        }

        /// <summary>
        /// A cell that makes <paramref name="player"/> win immediately, tried and undone via
        /// <see cref="TicTacToeGame.PlaceMarkForSearch"/>/<see cref="TicTacToeGame.EmptyCell"/> — never
        /// <see cref="TicTacToeGame.Move"/>, which would broadcast every trial cell to both real
        /// players as if it were an actual move (see that method's own comment). Null if no single
        /// move wins for <paramref name="player"/> right now.
        /// </summary>
        private (int Row, int Column)? FindWinningCellFor(Player player)
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);

            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    if (!TicTacToeGame.CellIsEmpty(row, column))
                        continue;

                    TicTacToeGame.PlaceMarkForSearch(player, row, column);
                    var wins = TicTacToeGame.CheckWinner(player);
                    TicTacToeGame.EmptyCell(row, column);

                    if (wins)
                        return (row, column);
                }
            }

            return null;
        }

        /// <summary>Takes an immediate win if there is one; otherwise blocks the opponent's immediate win. Null if neither applies.</summary>
        private (int Row, int Column)? BlockOrWin()
        {
            ArgumentNullException.ThrowIfNull(_otherPlayer);

            return FindWinningCellFor(this) ?? FindWinningCellFor(_otherPlayer);
        }

        private (int Row, int Column) ChooseBestCell()
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);

            var bestScore = int.MinValue;
            int moveRow = -1, moveColumn = -1;

            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    if (!TicTacToeGame.CellIsEmpty(row, column))
                        continue;

                    TicTacToeGame.PlaceMarkForSearch(this, row, column);
                    var score = Minimax(false, int.MinValue, int.MaxValue);
                    TicTacToeGame.EmptyCell(row, column);

                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    moveRow = row;
                    moveColumn = column;
                }
            }

            return (moveRow, moveColumn);
        }

        // Alpha-beta pruning: not needed for raw performance on a 3x3 board (unpruned minimax already
        // runs in milliseconds), but it's the standard, correct way to write minimax search, and it
        // keeps the search from wastefully exploring branches the opponent would never let it reach.
        // alpha is the best score the maximizer can already guarantee, beta the best the minimizer can
        // already guarantee; once beta <= alpha, this branch can't change the outcome the parent call
        // will pick, so both loop conditions below stop iterating for the remainder of this call.
        private int Minimax(bool isMaximizing, int alpha, int beta)
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);
            ArgumentNullException.ThrowIfNull(_otherPlayer);

            if (TicTacToeGame.CheckWinner(this)) return 1;
            if (TicTacToeGame.CheckWinner(_otherPlayer)) return -1;
            if (TicTacToeGame.IsBoardFull()) return 0;

            var bestScore = isMaximizing ? int.MinValue : int.MaxValue;

            for (var row = 0; row < 3 && beta > alpha; row++)
            {
                for (var column = 0; column < 3 && beta > alpha; column++)
                {
                    if (!TicTacToeGame.CellIsEmpty(row, column))
                        continue;

                    TicTacToeGame.PlaceMarkForSearch(isMaximizing ? this : _otherPlayer, row, column);
                    var score = Minimax(!isMaximizing, alpha, beta);
                    TicTacToeGame.EmptyCell(row, column);

                    if (isMaximizing)
                    {
                        bestScore = Math.Max(score, bestScore);
                        alpha = Math.Max(alpha, bestScore);
                    }
                    else
                    {
                        bestScore = Math.Min(score, bestScore);
                        beta = Math.Min(beta, bestScore);
                    }
                }
            }

            return bestScore;
        }

        public void ComputerMove()
        {
            ArgumentNullException.ThrowIfNull(TicTacToeGame);

            OnBeforePlayerMovedHandler();

            var (row, column) = _difficulty switch
            {
                DifficultyLevel.Easy => ChooseRandomCell(),
                DifficultyLevel.Medium => BlockOrWin() ?? ChooseRandomCell(),
                DifficultyLevel.Hard => ChooseBestCell(),
                _ => throw new ArgumentOutOfRangeException(nameof(_difficulty), _difficulty, "Unsupported difficulty level.")
            };

            // The only real, notifying move for this whole turn — every candidate cell considered
            // above was tried and undone through PlaceMarkForSearch/EmptyCell, never Move, so this is
            // the first and only BoardChanged this turn actually raises.
            TicTacToeGame.Move(this, row, column);

            OnPlayerMoved();
        }
    }
}
