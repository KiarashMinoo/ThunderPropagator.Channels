using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.Providers.DotNet.Quiz
{
    /// <summary>
    /// The typed message a host application constructs to publish externally-produced quiz state
    /// through <see cref="QuizProvider"/> — #194's own scope: "Define a typed provider message
    /// compatible with the quiz channel contract." Field-for-field identical to
    /// <see cref="QuizProviderPublishRequest"/> (which this maps onto losslessly, #194's own AC), kept
    /// as this package's own type rather than reusing that one directly so a host consuming only this
    /// provider package never needs to know that request type exists.
    /// </summary>
    public sealed record QuizProviderMessage
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

        internal QuizProviderPublishRequest ToPublishRequest() => new()
        {
            GameId = GameId,
            Phase = Phase,
            QuestionText = QuestionText,
            Options = Options,
            TimeRemaining = TimeRemaining,
            QuestionIndex = QuestionIndex,
            TotalQuestions = TotalQuestions,
            Scoreboard = Scoreboard,
            CorrectAnswer = CorrectAnswer,
            Winner = Winner
        };
    }
}
