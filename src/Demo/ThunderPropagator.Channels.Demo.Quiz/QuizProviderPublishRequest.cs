using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// The public surface #194's provider package maps its own <c>QuizProviderMessage</c> onto before
    /// calling <see cref="QuizChannel.PublishProviderState"/> — mirrors <see cref="QuizChannelFeederMessage"/>'s
    /// own public properties field-for-field (the "losslessly" #194's own AC asks for), since that type
    /// itself is internal and can never be constructed outside this assembly. Every field left at its
    /// default is exactly as meaningful as it is on the wire message itself: an empty
    /// <see cref="QuestionText"/>/<see cref="Options"/> outside Question/Revealing, an empty
    /// <see cref="Scoreboard"/> before anything has been scored, and so on — see those properties' own
    /// documentation on <see cref="QuizChannelFeederMessage"/> for the exact rules
    /// <see cref="QuizChannel.PublishProviderState"/> enforces on top of this.
    /// </summary>
    public sealed record QuizProviderPublishRequest
    {
        public required string GameId { get; init; }
        public required QuizPhase Phase { get; init; }
        public string QuestionText { get; init; } = string.Empty;
        public IReadOnlyList<string> Options { get; init; } = [];
        public int TimeRemaining { get; init; }
        public int QuestionIndex { get; init; }
        public int TotalQuestions { get; init; }
        public IReadOnlyList<QuizScoreboardEntry> Scoreboard { get; init; } = [];
        public string CorrectAnswer { get; init; } = string.Empty;
        public string Winner { get; init; } = string.Empty;
    }
}
