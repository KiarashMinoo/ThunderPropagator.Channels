namespace ThunderPropagator.Channels.Games.RockPaperScissors.Models
{
    // Issue #288: the persisted replacement for the old in-memory-only RockPaperScissorsGameSession —
    // a flat, plain-data snapshot of a resolved match (no live Player/Subscription references, which
    // aren't persistable and wouldn't survive a node's own process anyway). Kept purely server-side,
    // same as its predecessor — the wire protocol (RockPaperScissorsChannelFeederMessage) carries no
    // SessionId field, and this ticket does not add one to it.
    public
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsGameSessionRecord
    {
        public string SessionId { get; } = null!;
        public string FirstPlayerName { get; } = null!;
        public PlayerType FirstPlayerType { get; }
        public MoveKind FirstPlayerMove { get; }
        public string? FirstPlayerConnectionId { get; }
        public string SecondPlayerName { get; } = null!;
        public PlayerType SecondPlayerType { get; }
        public MoveKind SecondPlayerMove { get; }
        public string? SecondPlayerConnectionId { get; }
        public DateTimeOffset PlayedAt { get; }

        private RockPaperScissorsGameSessionRecord()
        {
        }

        private RockPaperScissorsGameSessionRecord(
            string sessionId,
            string firstPlayerName, PlayerType firstPlayerType, MoveKind firstPlayerMove, string? firstPlayerConnectionId,
            string secondPlayerName, PlayerType secondPlayerType, MoveKind secondPlayerMove, string? secondPlayerConnectionId,
            DateTimeOffset playedAt)
        {
            SessionId = sessionId;
            FirstPlayerName = firstPlayerName;
            FirstPlayerType = firstPlayerType;
            FirstPlayerMove = firstPlayerMove;
            FirstPlayerConnectionId = firstPlayerConnectionId;
            SecondPlayerName = secondPlayerName;
            SecondPlayerType = secondPlayerType;
            SecondPlayerMove = secondPlayerMove;
            SecondPlayerConnectionId = secondPlayerConnectionId;
            PlayedAt = playedAt;
        }

        internal static RockPaperScissorsGameSessionRecord Create(Player firstPlayer, Player secondPlayer) => new(
            Guid.NewGuid().ToString("N"),
            firstPlayer.Name, firstPlayer.PlayerType, firstPlayer.Move, firstPlayer.Subscription?.ConnectionInfo.ConnectionId,
            secondPlayer.Name, secondPlayer.PlayerType, secondPlayer.Move, secondPlayer.Subscription?.ConnectionInfo.ConnectionId,
            DateTimeOffset.UtcNow);
    }
}
