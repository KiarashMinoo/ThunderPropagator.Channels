using RapidStreamer.Channels.Games.TicTacToe.Game.Enums;

namespace RapidStreamer.Channels.Games.TicTacToe.Game.Players
{
    internal abstract class Player
    {
        protected TicTacToeGame? TicTacToeGame;

        public abstract PlayerKind Kind { get; }
        public PlayerSign Sign { get; }
        public string Name { get; }
        public string? ConnectionId { get; }

        public event EventHandler? BeforePlayerMovedHandler;
        public event EventHandler? PlayerMovedHandler;
        public event EventHandler<bool>? NotifyIsWonHandler;
        public event EventHandler? NotifyIsDrawnHandler;

        protected Player(string name, PlayerSign sign, string? connectionId)
        {
            Name = name;
            Sign = sign;
            ConnectionId = connectionId;
        }

        internal virtual void SetTicTacToeGame(TicTacToeGame ticTacToeGame)
        {
            TicTacToeGame = ticTacToeGame;
        }

        protected virtual void OnBeforePlayerMovedHandler() => BeforePlayerMovedHandler?.Invoke(this, EventArgs.Empty);
        protected virtual void OnPlayerMoved() => PlayerMovedHandler?.Invoke(this, EventArgs.Empty);
        internal void NotifyIsWon(bool e) => NotifyIsWonHandler?.Invoke(this, e);
        internal void NotifyIsDrawn() => NotifyIsDrawnHandler?.Invoke(this, EventArgs.Empty);
    }
}