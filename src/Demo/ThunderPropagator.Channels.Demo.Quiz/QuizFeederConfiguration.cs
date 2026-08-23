using ThunderPropagator.Application.Feeders;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// The configurable durations <see cref="QuizFeeder"/>'s game loop waits at each phase — #189's
    /// own AC: "Default flow uses 10s lobby, 15s question, 3s reveal, and 5s scoreboard durations
    /// unless configured otherwise." Each duration must be strictly positive; a zero or negative value
    /// would make the loop advance with no delay at all, which is exactly the busy-spin #189's own AC
    /// forbids.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class QuizFeederConfiguration : AbstractFeederConfiguration
    {
        /// <summary>How long a session waits in <see cref="Game.Enums.QuizPhase.Lobby"/> before the loop starts the game on its own. Default: 10 seconds.</summary>
        public TimeSpan LobbyDuration
        {
            get => Get(TimeSpan.FromSeconds(10));
            set => Set(ValidatePositive(value, nameof(LobbyDuration)));
        }

        /// <summary>How long each question stays open for answers before the loop reveals its answer. Default: 15 seconds.</summary>
        public TimeSpan QuestionDuration
        {
            get => Get(TimeSpan.FromSeconds(15));
            set => Set(ValidatePositive(value, nameof(QuestionDuration)));
        }

        /// <summary>How long the correct answer is shown before the loop moves on to the scoreboard. Default: 3 seconds.</summary>
        public TimeSpan RevealingDuration
        {
            get => Get(TimeSpan.FromSeconds(3));
            set => Set(ValidatePositive(value, nameof(RevealingDuration)));
        }

        /// <summary>How long standings are shown before the loop starts the next question or ends the game. Default: 5 seconds.</summary>
        public TimeSpan ScoreboardDuration
        {
            get => Get(TimeSpan.FromSeconds(5));
            set => Set(ValidatePositive(value, nameof(ScoreboardDuration)));
        }

        /// <summary>
        /// Number of questions played per game, taken from the front of that game's own shuffled
        /// ordering (see <see cref="Game.QuizQuestionBank.Shuffle"/>) rather than the bank's fixed
        /// order (#188). Default: <see cref="int.MaxValue"/> — every question in the configured bank
        /// is played, matching this package's documented demo flow (#189) unless a host opts into a
        /// shorter game explicitly. Must be strictly positive; a value larger than the bank's own
        /// question count is not an error — <see cref="Game.QuizGameLoop"/> plays the whole bank
        /// instead of throwing, since the bank itself is supplied independently of this configuration.
        /// </summary>
        public int QuestionsPerGame
        {
            get => Get(int.MaxValue);
            set => Set(ValidatePositive(value, nameof(QuestionsPerGame)));
        }

        public QuizFeederConfiguration()
        {
            IsEnabled = true;
        }

        internal void Bind(QuizFeederConfiguration quizFeederConfiguration) => base.Bind(quizFeederConfiguration);

        private static TimeSpan ValidatePositive(TimeSpan value, string propertyName)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero, propertyName);
            return value;
        }

        private static int ValidatePositive(int value, string propertyName)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0, propertyName);
            return value;
        }
    }
}
