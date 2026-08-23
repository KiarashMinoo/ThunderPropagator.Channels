namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Thrown by <see cref="QuizChannel.Join"/> when a game has left its Lobby phase and
    /// <see cref="QuizChannelConfiguration.AllowMidGameJoin"/> is <see langword="false"/> — the
    /// configured policy for #191's own "non-lobby joins according to configuration" scope. When
    /// <see cref="QuizChannelConfiguration.AllowMidGameJoin"/> is <see langword="true"/> (the default),
    /// a mid-game join is never rejected for this reason.
    /// </summary>
    public sealed class QuizNonLobbyJoinNotAllowedException(string gameId)
        : Exception($"Game '{gameId}' is no longer in its Lobby phase, and this deployment does not allow joining a game already in progress.")
    {
        public string GameId { get; } = gameId;
    }
}
