namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Thrown by <see cref="QuizChannel.StartGame"/> when fewer than <see cref="QuizChannelConfiguration.MinPlayers"/>
    /// players are currently connected to <see cref="GameId"/> — #193's own AC: validate "minimum
    /// players" before starting.
    /// </summary>
    public sealed class QuizNotEnoughPlayersException(string gameId, int minPlayers, int connectedPlayers)
        : Exception($"Game '{gameId}' needs at least {minPlayers} connected player(s) to start (has {connectedPlayers}).")
    {
        public string GameId { get; } = gameId;
        public int MinPlayers { get; } = minPlayers;
        public int ConnectedPlayers { get; } = connectedPlayers;
    }
}
