namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Thrown by <see cref="QuizChannel.StartGame"/> when the calling connection is a joined player in
    /// <see cref="GameId"/> but not its host — #193's own AC: "Only the host can start a game." Distinct
    /// from <see cref="QuizNotAJoinedPlayerException"/> (which covers a connection with no established
    /// identity in the game at all): this one names specifically which authorization failed.
    /// </summary>
    public sealed class QuizNotTheHostException(string gameId) : Exception($"The calling connection is not the host of game '{gameId}'.")
    {
        public string GameId { get; } = gameId;
    }
}
