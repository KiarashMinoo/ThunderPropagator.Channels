namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Thrown by <see cref="QuizChannel.SubmitAnswer"/> when the calling connection is not a currently
    /// joined player in <see cref="GameId"/> — #192's own AC: "Only joined players can answer their
    /// active game/question." This is an authorization failure, not a game-flow outcome: unlike a
    /// late, stale, invalid, or duplicate answer (all represented by <see cref="Game.Enums.QuizAnswerOutcome"/>,
    /// since a genuinely joined player can legitimately hit any of those), a connection with no
    /// established identity in this game has no standing to submit an answer at all.
    /// </summary>
    public sealed class QuizNotAJoinedPlayerException(string gameId) : Exception($"The calling connection is not a joined player in game '{gameId}'.")
    {
        public string GameId { get; } = gameId;
    }
}
