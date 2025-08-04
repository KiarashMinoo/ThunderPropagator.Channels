using RapidStreamer.Channels.Games.TicTacToe.Game.Exceptions;
using RapidStreamer.Channels.Games.TicTacToe.Game.Players;

namespace RapidStreamer.Channels.Games.TicTacToe.Game
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

        public void StartGame(Player player)
        {
            Player2 = player;
            Player2.SetTicTacToeGame(this);

            Player1.BeforePlayerMovedHandler += PlayerOnBeforePlayerMovedHandler;
            Player2.BeforePlayerMovedHandler += PlayerOnBeforePlayerMovedHandler;

            Player1.PlayerMovedHandler += PlayerOnPlayerMovedHandler;
            Player2.PlayerMovedHandler += PlayerOnPlayerMovedHandler;
        }

        internal bool CellIsEmpty(int row, int column) => _board[row][column] == null;
        internal void EmptyCell(int row, int column) => _board[row][column] = null;

        internal void Move(Player player, int row, int column)
        {
            _board[row][column] = player;
            OnBoardChanged(new BoardChangedEventArgs(player, row, column));
        }
    }
}