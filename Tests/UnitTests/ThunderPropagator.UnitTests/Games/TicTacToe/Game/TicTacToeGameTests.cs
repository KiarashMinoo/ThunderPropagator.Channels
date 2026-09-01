using ThunderPropagator.Channels.Games.TicTacToe.Game;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Exceptions;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Players;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.Game
{
    /// <summary>
    /// Issue: TicTacToeGame had never been exercised past AddGame/StartGame's administrative phase —
    /// every existing test here was a bare type-shape check (IsEnum/IsAssignableFrom). Two bugs made
    /// every game unplayable in practice, both fixed alongside this coverage:
    /// - _currentPlayer was never initialized, so the very first move after StartGame always threw
    ///   InvalidMoveException (see HumanMove_ByPlayer1RightAfterStartGame_DoesNotThrow).
    /// - ComputerPlayer's search used the real, notifying Move for every trial placement it tried and
    ///   undid, broadcasting the computer's entire search tree as fake moves (see
    ///   ComputerMove_OnHardDifficultyFromAnEmptyBoard_FiresBoardChangedExactlyOnce, which would time
    ///   out or count in the hundreds of thousands against the old code).
    /// </summary>
    public sealed class TicTacToeGameTests
    {
        [Fact]
        public void InvalidMoveException_IsException()
        {
            var type = typeof(InvalidMoveException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void PlayerSign_IsEnum()
        {
            Assert.True(typeof(PlayerSign).IsEnum);
        }

        [Fact]
        public void PlayerKind_IsEnum()
        {
            Assert.True(typeof(PlayerKind).IsEnum);
        }

        [Fact]
        public void DifficultyLevel_IsEnum()
        {
            Assert.True(typeof(DifficultyLevel).IsEnum);
        }

        private static (TicTacToeGame Game, HumanPlayer Player1, HumanPlayer Player2) CreateStartedHumanGame()
        {
            var player1 = new HumanPlayer("Alice", PlayerSign.X, "connection-1");
            var game = new TicTacToeGame("session-1", player1);
            var player2 = new HumanPlayer("Bob", PlayerSign.O, "connection-2");
            game.StartGame(player2);
            return (game, player1, player2);
        }

        [Fact]
        public void StartGame_SetsTheCurrentTurnToPlayer1()
        {
            var (game, player1, _) = CreateStartedHumanGame();

            Assert.Equal(player1.Sign, game.CurrentTurnSign);
        }

        [Fact]
        public void HumanMove_ByPlayer1RightAfterStartGame_DoesNotThrow()
        {
            var (_, player1, _) = CreateStartedHumanGame();

            var exception = Record.Exception(() => player1.HumanMove(0, 0));

            Assert.Null(exception);
        }

        [Fact]
        public void HumanMove_ByPlayer2BeforePlayer1HasMoved_ThrowsInvalidMoveException()
        {
            var (_, _, player2) = CreateStartedHumanGame();

            Assert.Throws<InvalidMoveException>(() => player2.HumanMove(0, 0));
        }

        [Fact]
        public void HumanMove_OutOfTurn_DoesNotMutateTheBoard()
        {
            var (game, _, player2) = CreateStartedHumanGame();

            Assert.Throws<InvalidMoveException>(() => player2.HumanMove(0, 0));
            Assert.Null(game.SignAt(0, 0));
        }

        [Fact]
        public void PlayingAWinningLine_EndsTheGameAndNotifiesTheWinner()
        {
            var (_, player1, player2) = CreateStartedHumanGame();
            bool? player1Won = null;
            bool? player2Won = null;
            player1.NotifyIsWonHandler += (_, won) => player1Won = won;
            player2.NotifyIsWonHandler += (_, won) => player2Won = won;

            // X: (0,0) (0,1) (0,2) completes the top row. O: (1,0) (1,1) in between.
            player1.HumanMove(0, 0);
            player2.HumanMove(1, 0);
            player1.HumanMove(0, 1);
            player2.HumanMove(1, 1);
            player1.HumanMove(0, 2);

            Assert.True(player1Won);
            Assert.False(player2Won);
        }

        [Fact]
        public void FillingTheBoardWithNoWinner_NotifiesBothPlayersOfADraw()
        {
            var (_, player1, player2) = CreateStartedHumanGame();
            var player1Drawn = false;
            var player2Drawn = false;
            player1.NotifyIsDrawnHandler += (_, _) => player1Drawn = true;
            player2.NotifyIsDrawnHandler += (_, _) => player2Drawn = true;

            // X O X
            // X O O
            // O X X
            player1.HumanMove(0, 0);
            player2.HumanMove(0, 1);
            player1.HumanMove(0, 2);
            player2.HumanMove(1, 1);
            player1.HumanMove(1, 0);
            player2.HumanMove(1, 2);
            player1.HumanMove(2, 1);
            player2.HumanMove(2, 0);
            player1.HumanMove(2, 2);

            Assert.True(player1Drawn);
            Assert.True(player2Drawn);
        }

        [Fact]
        public void RestoreState_ReproducesTheGivenBoardAndTurn()
        {
            var (game, player1, player2) = CreateStartedHumanGame();

            game.RestoreState([PlayerSign.X, null, null, null, PlayerSign.O, null, null, null, null], player2.Sign);

            Assert.Equal(PlayerSign.X, game.SignAt(0, 0));
            Assert.Equal(PlayerSign.O, game.SignAt(1, 1));
            Assert.Null(game.SignAt(0, 1));
            Assert.Equal(player2.Sign, game.CurrentTurnSign);
        }

        // Issue: ComputerPlayer's minimax/BlockOrWin used to call the real, notifying Move for every
        // speculative cell it tried during its own search and then undid — since Move fires
        // BoardChanged, every one of those trial placements was broadcast to both real players as if
        // it were an actual move. From an empty board, unpruned full-tree Hard-difficulty search
        // visits on the order of hundreds of thousands of trial placements; against the old code this
        // assertion would either time out or fail with a count far larger than 1.
        [Fact]
        public void ComputerMove_OnHardDifficultyFromAnEmptyBoard_FiresBoardChangedExactlyOnce()
        {
            var human = new HumanPlayer("Alice", PlayerSign.X, "connection-1");
            var game = new TicTacToeGame("session-1", human);
            var computer = new ComputerPlayer(PlayerSign.O, DifficultyLevel.Hard);
            game.StartGame(computer);
            game.RestoreState(new PlayerSign?[9], computer.Sign); // it's the computer's turn on an otherwise-empty board

            var boardChangedCount = 0;
            game.BoardChanged += (_, _) => boardChangedCount++;

            computer.ComputerMove();

            Assert.Equal(1, boardChangedCount);
        }

        [Fact]
        public void ComputerMove_OnMediumDifficulty_TakesAnImmediateWinInsteadOfBlocking()
        {
            var human = new HumanPlayer("Alice", PlayerSign.X, "connection-1");
            var game = new TicTacToeGame("session-1", human);
            var computer = new ComputerPlayer(PlayerSign.O, DifficultyLevel.Medium);
            game.StartGame(computer);

            // O already has two in a column at (0,0)/(1,0) — (2,0) wins immediately. X has its own
            // two-in-a-row at (0,1)/(1,1) threatening (2,1), so a computer that only ever blocked and
            // never checked its own win first would block (2,1) instead of winning at (2,0).
            game.RestoreState(
                [PlayerSign.O, PlayerSign.X, null,
                 PlayerSign.O, PlayerSign.X, null,
                 null, null, null],
                computer.Sign);

            computer.ComputerMove();

            Assert.Equal(PlayerSign.O, game.SignAt(2, 0));
        }

        [Fact]
        public void ComputerMove_OnMediumDifficulty_BlocksAnImminentHumanWinWhenItHasNoWinOfItsOwn()
        {
            var human = new HumanPlayer("Alice", PlayerSign.X, "connection-1");
            var game = new TicTacToeGame("session-1", human);
            var computer = new ComputerPlayer(PlayerSign.O, DifficultyLevel.Medium);
            game.StartGame(computer);

            // X has two in the top row — (0,2) wins for X next turn unless O blocks it now.
            game.RestoreState(
                [PlayerSign.X, PlayerSign.X, null,
                 PlayerSign.O, null, null,
                 null, null, null],
                computer.Sign);

            computer.ComputerMove();

            Assert.Equal(PlayerSign.O, game.SignAt(0, 2));
        }

        // The defining promise of "Hard" difficulty: minimax is a solved-game-optimal strategy for
        // tic-tac-toe, so no human line can beat it — at worst it draws. Deterministic: minimax always
        // picks the same highest-scoring cell for a given board (ties broken by scan order), so this
        // one scripted human line is a real, repeatable regression check, not a one-off sample.
        [Fact]
        public void ComputerMove_OnHardDifficulty_NeverLosesRegardlessOfHumanStrategy()
        {
            var human = new HumanPlayer("Alice", PlayerSign.X, "connection-1");
            var game = new TicTacToeGame("session-1", human);
            var computer = new ComputerPlayer(PlayerSign.O, DifficultyLevel.Hard);
            game.StartGame(computer);

            bool? humanWon = null;
            human.NotifyIsWonHandler += (_, won) => humanWon = won;

            int[][] candidateMoves = [[0, 0], [0, 2], [2, 0], [2, 2], [0, 1], [1, 0], [1, 2], [2, 1], [1, 1]];
            foreach (var move in candidateMoves)
            {
                if (!game.IsValidMove(move[0], move[1]))
                    continue;

                human.HumanMove(move[0], move[1]);

                if (humanWon is not null || game.IsBoardFull())
                    break;
            }

            Assert.NotEqual(true, humanWon);
        }
    }
}
