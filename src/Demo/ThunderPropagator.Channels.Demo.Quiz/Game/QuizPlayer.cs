namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// One player's membership in a single <see cref="QuizGameSession"/>: their display name (the
    /// stable identity a reconnect is matched against), their current connection, whether they are
    /// the session's host, and whether that connection is presently live. Every mutable member here
    /// is only ever written while the owning <see cref="QuizGameSession"/> holds its own lock — this
    /// type has no synchronization of its own.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizPlayer(string playerName, string connectionId, bool isHost)
    {
        /// <summary>The display name a player joined with — the identity a later reconnect is matched against, independent of connection.</summary>
        public string PlayerName { get; } = playerName;

        /// <summary>The connection currently associated with this player. Replaced, not appended to, on reconnect — see <see cref="QuizGameSession.Join"/>.</summary>
        public string ConnectionId { get; internal set; } = connectionId;

        /// <summary>Whether this player is the session's host — fixed at first join and never reassigned by <see cref="QuizGameSession"/> itself; see that type's own remarks for why.</summary>
        public bool IsHost { get; } = isHost;

        /// <summary>Whether <see cref="ConnectionId"/> is presently live. False between a disconnect and a matching reconnect.</summary>
        public bool IsConnected { get; internal set; } = true;
    }
}
