namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// One player's standing, as carried by <see cref="QuizChannelFeederMessage.Scoreboard"/>. Public
    /// (unlike most of this assembly's internal simulation types) because #194's own
    /// <see cref="QuizProviderPublishRequest.Scoreboard"/> — an external host's own scoring, entirely
    /// independent of this assembly's built-in <c>QuizScoringEngine</c> — needs the exact same shape to
    /// map losslessly onto the wire contract.
    /// </summary>
    public sealed record QuizScoreboardEntry(string PlayerName, int Score);
}
