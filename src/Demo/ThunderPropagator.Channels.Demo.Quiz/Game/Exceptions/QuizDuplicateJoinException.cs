namespace ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="QuizGameSession.Join"/> when a player name is already connected to the
    /// session under a different connection — two live connections can never claim the same display
    /// name in the same game at once. This is distinct from a reconnect: a player name whose existing
    /// connection has already disconnected is not a duplicate, it is a reconnect (see
    /// <see cref="QuizGameSession.Join"/>'s own remarks), and succeeds rather than throwing.
    /// </summary>
    public sealed class QuizDuplicateJoinException(string gameId, string playerName)
        : Exception($"Player '{playerName}' is already connected to game '{gameId}'.")
    {
        public string GameId { get; } = gameId;
        public string PlayerName { get; } = playerName;
    }
}
