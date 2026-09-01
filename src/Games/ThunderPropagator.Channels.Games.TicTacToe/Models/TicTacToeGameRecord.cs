using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;

namespace ThunderPropagator.Channels.Games.TicTacToe.Models
{
    // Issue: the persisted replacement for TicTacToeChannel's old node-local _games dictionary — a
    // request landing on a different cluster node than the one a game was created/last moved on found
    // nothing there. Unlike RockPaperScissorsGameSessionRecord (a write-once historical log), this
    // entity is mutated on every move — Start/ApplyMove below update it in place, the same
    // fetch-mutate-UpdateAsync shape UserService/GroupService already use for Chat.
    //
    // Board is a 9-character row-major string ('X'/'O'/'-') rather than a nested board structure —
    // trivially persistable as a single column/field across all three providers, and simple enough
    // that no dedicated (de)serializer is worth the complexity a richer shape would need.
    //
    // Concurrency note: no version/ETag column guards SaveGameAsync against two concurrent moves on
    // the same session racing each other (the same accepted trade-off ChatUserSessionService.LogOutAsync's
    // own comment documents for a narrower case). Turn enforcement already rejects an out-of-turn move
    // once one of a genuine race's two writes lands; a true "same instant, same connection, duplicate
    // request" double-apply is a narrow, low-severity edge case (a network retry at worst), not
    // something this fix adds new exposure to — the original in-memory version had no protection
    // against that scenario either, since a single node's dictionary was never a linearizability
    // guarantee for concurrent requests on the same entry.
    public
#if !DEBUG
        sealed
#endif
        class TicTacToeGameRecord
    {
        public string SessionId { get; } = null!;
        public string Board { get; private set; } = null!;
        public string Player1Name { get; } = null!;
        public PlayerSign Player1Sign { get; }
        public string Player1ConnectionId { get; } = null!;
        public string? Player2Name { get; private set; }
        public PlayerKind? Player2Kind { get; private set; }
        public string? Player2ConnectionId { get; private set; }
        public DifficultyLevel? Player2DifficultyLevel { get; private set; }
        public PlayerSign? CurrentTurnSign { get; private set; }

        private TicTacToeGameRecord()
        {
        }

        private TicTacToeGameRecord(string sessionId, string board, string player1Name, PlayerSign player1Sign, string player1ConnectionId) : this()
        {
            SessionId = sessionId;
            Board = board;
            Player1Name = player1Name;
            Player1Sign = player1Sign;
            Player1ConnectionId = player1ConnectionId;
        }

        /// <summary>A freshly created game with only Player1 present — not yet joinable-and-playable until <see cref="Start"/>.</summary>
        internal static TicTacToeGameRecord CreateWaitingForOpponent(string sessionId, string board, string player1Name, PlayerSign player1Sign, string player1ConnectionId)
            => new(sessionId, board, player1Name, player1Sign, player1ConnectionId);

        /// <summary>Assigns Player2 and the initial turn — called once, whether Player2 is a joining human or the computer opponent created alongside Player1.</summary>
        internal void Start(string board, string player2Name, PlayerKind player2Kind, string? player2ConnectionId, DifficultyLevel? player2DifficultyLevel, PlayerSign currentTurnSign)
        {
            Board = board;
            Player2Name = player2Name;
            Player2Kind = player2Kind;
            Player2ConnectionId = player2ConnectionId;
            Player2DifficultyLevel = player2DifficultyLevel;
            CurrentTurnSign = currentTurnSign;
        }

        /// <summary>Records a move's resulting board and next turn — called after every accepted move, human or computer, right up until the game ends (at which point the record is deleted, not updated).</summary>
        internal void ApplyMove(string board, PlayerSign currentTurnSign)
        {
            Board = board;
            CurrentTurnSign = currentTurnSign;
        }
    }
}
