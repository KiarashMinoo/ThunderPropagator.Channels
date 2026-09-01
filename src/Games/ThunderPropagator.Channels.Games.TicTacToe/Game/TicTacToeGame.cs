using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Exceptions;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Players;

namespace ThunderPropagator.Channels.Games.TicTacToe.Game
{
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeGame
    {
        private readonly IList<IList<Player?>> _board;
        private Player? _currentPlayer;

        public string SessionId { get; }
        public Player Player1 { get; }
        public Player Player2 { get; private set; } = null!;

        public event EventHandler<BoardChangedEventArgs>? BoardChanged;
        public event EventHandler? GameEnded;

        public TicTacToeGame(string sessionId, Player player1)
        {
            SessionId = sessionId;
            Player1 = player1;
            Player1.SetTicTacToeGame(this);

            _board = [];
            _board.Add(new List<Player?> { null, null, null }); //Row1
            _board.Add(new List<Player?> { null, null, null }); //Row2
            _board.Add(new List<Player?> { null, null, null }); //Row3
        }

        private void OnBoardChanged(BoardChangedEventArgs e) => BoardChanged?.Invoke(this, e);
        private void OnGameEnded() => GameEnded?.Invoke(this, EventArgs.Empty);

        private void PlayerOnBeforePlayerMovedHandler(object? sender, EventArgs e)
        {
            var player = (Player)sender!;
            if (_currentPlayer != player)
                throw new InvalidMoveException();
        }

        private void PlayerOnPlayerMovedHandler(object? sender, EventArgs e)
        {
            var player = (Player)sender!;
            if (CheckWinner(player))
            {
                if (player == Player1)
                {
                    Player1.NotifyIsWon(true);
                    Player2.NotifyIsWon(false);
                }
                else
                {
                    Player1.NotifyIsWon(false);
                    Player2.NotifyIsWon(true);
                }

                OnGameEnded();
                return;
            }

            if (IsBoardFull())
            {
                Player1.NotifyIsDrawn();
                Player2.NotifyIsDrawn();

                OnGameEnded();
                return;
            }

            var otherPlayer = _currentPlayer = player == Player1 ? Player2 : Player1;

            if (player is HumanPlayer && otherPlayer is ComputerPlayer computerPlayer)
                computerPlayer.ComputerMove();
        }

        internal bool IsValidMove(int row, int column) => row is >= 0 and < 3 && column is >= 0 and < 3 && _board[row][column] == null!;

        internal bool IsBoardFull() => _board.All(row => row.All(column => column != null));

        internal bool CheckWinner(Player player)
        {
            for (int i = 0; i < 3; i++)
            {
                if (_board[i][0] == player && _board[i][1] == player && _board[i][2] == player) return true;
                if (_board[0][i] == player && _board[1][i] == player && _board[2][i] == player) return true;
            }

            if (_board[0][0] == player && _board[1][1] == player && _board[2][2] == player) return true;
            if (_board[0][2] == player && _board[1][1] == player && _board[2][0] == player) return true;

            return false;
        }

        // Bug fix: _currentPlayer was never initialized anywhere before this fix, so the very first
        // move after both players joined always failed PlayerOnBeforePlayerMovedHandler's turn check
        // (null != player is always true) and threw InvalidMoveException — every game, human-vs-human
        // or human-vs-computer, was unplayable past this point. Player1 always moves first, matching
        // the pre-existing (if previously unreachable) implicit convention: nothing elsewhere in this
        // codebase ties move order to PlayerSign, only to which player object calls Move first.
        public void StartGame(Player player)
        {
            Player2 = player;
            Player2.SetTicTacToeGame(this);

            Player1.BeforePlayerMovedHandler += PlayerOnBeforePlayerMovedHandler;
            Player2.BeforePlayerMovedHandler += PlayerOnBeforePlayerMovedHandler;

            Player1.PlayerMovedHandler += PlayerOnPlayerMovedHandler;
            Player2.PlayerMovedHandler += PlayerOnPlayerMovedHandler;

            _currentPlayer = Player1;
        }

        internal bool CellIsEmpty(int row, int column) => _board[row][column] == null;
        internal void EmptyCell(int row, int column) => _board[row][column] = null;

        /// <summary>
        /// The real, notifying move — fires <see cref="BoardChanged"/> so subscribers actually see it.
        /// Used only for a player's own genuine move (<see cref="HumanPlayer.HumanMove"/>,
        /// <see cref="ComputerPlayer.ComputerMove"/>'s final chosen cell) — never for the AI's internal
        /// what-if search, which uses <see cref="PlaceMarkForSearch"/> instead. See that method's own
        /// comment for why conflating the two used to broadcast the computer's entire search tree to
        /// both players.
        /// </summary>
        internal void Move(Player player, int row, int column)
        {
            _board[row][column] = player;
            OnBoardChanged(new BoardChangedEventArgs(player, row, column));
        }

        /// <summary>
        /// Bug fix: <see cref="ComputerPlayer"/>'s search (BlockOrWin, minimax) used to call
        /// <see cref="Move"/> for every speculative trial placement it tried and then undid via
        /// <see cref="EmptyCell"/> — since Move fires <see cref="BoardChanged"/>, every one of those
        /// trial placements was broadcast to both real players as if it were an actual move. On Hard
        /// difficulty (full minimax, no pruning needed for a 3x3 board but still explores the whole
        /// remaining game tree) a single computer turn could emit tens of thousands of these before
        /// settling on its real move. This mutates the board silently — callers doing their own search
        /// are responsible for calling <see cref="EmptyCell"/> to undo it themselves.
        /// </summary>
        internal void PlaceMarkForSearch(Player player, int row, int column) => _board[row][column] = player;

        /// <summary>The sign occupying (<paramref name="row"/>, <paramref name="column"/>), or null if empty — used to snapshot the board for persistence.</summary>
        internal PlayerSign? SignAt(int row, int column) => _board[row][column]?.Sign;

        /// <summary>The sign whose turn it currently is, or null if the game hasn't started (no second player yet) — used to snapshot state for persistence.</summary>
        internal PlayerSign? CurrentTurnSign => _currentPlayer?.Sign;

        /// <summary>
        /// Restores board cells and whose turn it is from a previously persisted snapshot — the
        /// cluster-safe counterpart to <see cref="SignAt"/>/<see cref="CurrentTurnSign"/>. Must be
        /// called after <see cref="StartGame"/> (Player2 must already be set); overrides the
        /// Player1-moves-first default <see cref="StartGame"/> just set, since the persisted game may
        /// already be mid-play.
        /// </summary>
        internal void RestoreState(IReadOnlyList<PlayerSign?> cells, PlayerSign currentTurnSign)
        {
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    var sign = cells[row * 3 + column];
                    _board[row][column] = sign switch
                    {
                        null => null,
                        _ when sign == Player1.Sign => Player1,
                        _ when sign == Player2.Sign => Player2,
                        _ => throw new InvalidOperationException($"Sign '{sign}' matches neither player.")
                    };
                }
            }

            _currentPlayer = currentTurnSign == Player1.Sign ? Player1 : Player2;
        }
    }
}
