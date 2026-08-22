namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>One player's standing, as carried by <see cref="QuizChannelFeederMessage.Scoreboard"/>.</summary>
    internal sealed record QuizScoreboardEntry(string PlayerName, int Score);
}
