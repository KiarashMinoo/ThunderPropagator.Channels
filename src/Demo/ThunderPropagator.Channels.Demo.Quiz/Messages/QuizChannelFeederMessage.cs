using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Messages;
using ThunderPropagator.Channels.Demo.Quiz.Metadata;

namespace ThunderPropagator.Channels.Demo.Quiz.Messages
{
    // Issue #185: fields added here to give QuizChannelMetadata's descriptors real properties to
    // describe (see QuizChannelMetadata's own comment on why #185 couldn't wait for #186's full
    // serialization contract). Issue #186: validation added on top — every setter below rejects an
    // out-of-range/oversized value immediately (mirroring
    // NotificationsChannelFeederMessageValidationException's #68/#74 shape), so a malformed value can
    // never reach a subscriber in the first place, rather than being caught later at some emission
    // boundary a caller could forget to check.
    internal
#if !DEBUG
        sealed
#endif
        class QuizChannelFeederMessage : FeederMessage
    {
        /// <summary>Maximum allowed length of <see cref="GameId"/>.</summary>
        public const int GameIdMaxLength = 128;

        /// <summary>
        /// Maximum allowed length of any single piece of free text this message carries —
        /// <see cref="QuestionText"/>, <see cref="CorrectAnswer"/>, <see cref="Winner"/>, each entry
        /// in <see cref="Options"/>, and each <see cref="QuizScoreboardEntry.PlayerName"/> in
        /// <see cref="Scoreboard"/>. One shared constant rather than a distinct one per field, since
        /// none of them has a reason to be bounded differently from the others.
        /// </summary>
        public const int TextMaxLength = 500;

        /// <summary>Maximum number of answer choices <see cref="Options"/> may hold.</summary>
        public const int OptionsMaxCount = 10;

        /// <summary>Maximum number of players <see cref="Scoreboard"/> may report standings for.</summary>
        public const int ScoreboardMaxCount = 100;

        /// <summary>Identifies which game session this message belongs to — the only subscribing key.</summary>
        public string GameId
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(ValidateNonEmpty(value, nameof(GameId), GameIdMaxLength));
        }

        /// <summary>The game's current lifecycle phase — see <see cref="Game.QuizPhaseStateMachine"/>.</summary>
        public QuizPhase Phase
        {
            get => GetValueOrDefault(QuizPhase.Lobby);
            set => SetValue(value);
        }

        /// <summary>The current question's text. Empty in <see cref="QuizPhase.Lobby"/> and <see cref="QuizPhase.GameOver"/>.</summary>
        public string QuestionText
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(ValidateMaxLength(value, nameof(QuestionText), TextMaxLength));
        }

        /// <summary>The current question's answer choices. Empty in <see cref="QuizPhase.Lobby"/> and <see cref="QuizPhase.GameOver"/>.</summary>
        public IReadOnlyList<string> Options
        {
            get => GetValueOrNull<IReadOnlyList<string>>() ?? [];
            set => SetValue(ValidateOptions(value));
        }

        /// <summary>Seconds remaining in the current <see cref="QuizPhase.Question"/> countdown. Meaningless (0) outside that phase.</summary>
        public int TimeRemaining
        {
            get => GetValueOrDefault(0);
            set => SetValue(ValidateNonNegative(value, nameof(TimeRemaining)));
        }

        /// <summary>0-based index of the current question within the game.</summary>
        public int QuestionIndex
        {
            get => GetValueOrDefault(0);
            set => SetValue(ValidateNonNegative(value, nameof(QuestionIndex)));
        }

        /// <summary>Total number of questions in the game.</summary>
        public int TotalQuestions
        {
            get => GetValueOrDefault(0);
            set => SetValue(ValidateNonNegative(value, nameof(TotalQuestions)));
        }

        /// <summary>Current standings. Empty until at least one question has been scored.</summary>
        public IReadOnlyList<QuizScoreboardEntry> Scoreboard
        {
            get => GetValueOrNull<IReadOnlyList<QuizScoreboardEntry>>() ?? [];
            set => SetValue(ValidateScoreboard(value));
        }

        // Issue #185's own AC: "the answer is not exposed before the Revealing phase". Redacting on
        // read (rather than trusting every future caller to only assign this once Revealing is
        // reached) means the guarantee holds regardless of when a caller sets it — the underlying
        // stored value survives untouched, only what a reader observes is gated by Phase.
        /// <summary>
        /// The correct answer to the current question. Only ever readable once <see cref="Phase"/>
        /// has reached <see cref="QuizPhase.Revealing"/> (or later) — empty before that, regardless
        /// of what was assigned.
        /// </summary>
        public string CorrectAnswer
        {
            get => Phase is QuizPhase.Revealing or QuizPhase.Scoreboard or QuizPhase.GameOver
                ? GetValueOrDefault(string.Empty)
                : string.Empty;
            set => SetValue(ValidateMaxLength(value, nameof(CorrectAnswer), TextMaxLength));
        }

        // Issue #185's own AC only calls out CorrectAnswer explicitly, but the same "don't expose
        // ahead of the phase that reveals it" reasoning applies here — a Winner assigned early
        // (e.g. computed once but held until GameOver is announced) must not leak through a message
        // built for an earlier phase.
        /// <summary>
        /// The winning player's name. Only ever readable once <see cref="Phase"/> is
        /// <see cref="QuizPhase.GameOver"/> — empty before that, regardless of what was assigned.
        /// </summary>
        public string Winner
        {
            get => Phase == QuizPhase.GameOver ? GetValueOrDefault(string.Empty) : string.Empty;
            set => SetValue(ValidateMaxLength(value, nameof(Winner), TextMaxLength));
        }

        // Issue #186: GameId is the one field that must never be null/empty/whitespace — every other
        // string field here is legitimately empty at some phase (QuestionText/Options/CorrectAnswer/
        // Winner all default to empty in the phases that don't populate them yet), so only length is
        // bounded for those; GameId alone is required, since a message with no session identity
        // can't be routed to a subscriber at all.
        private static string ValidateNonEmpty(string value, string propertyName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new QuizChannelFeederMessageValidationException(propertyName, "must not be null, empty, or whitespace-only.");

            return ValidateMaxLength(value, propertyName, maxLength);
        }

        private static string ValidateMaxLength(string value, string propertyName, int maxLength)
        {
            if (value?.Length > maxLength)
                throw new QuizChannelFeederMessageValidationException(propertyName, $"must not exceed {maxLength} characters (was {value.Length}).");

            return value!;
        }

        private static int ValidateNonNegative(int value, string propertyName)
        {
            if (value < 0)
                throw new QuizChannelFeederMessageValidationException(propertyName, $"must not be negative (was {value}).");

            return value;
        }

        private static IReadOnlyList<string> ValidateOptions(IReadOnlyList<string> value)
        {
            if (value is null)
                return value!;

            if (value.Count > OptionsMaxCount)
                throw new QuizChannelFeederMessageValidationException(nameof(Options), $"must not contain more than {OptionsMaxCount} entries (had {value.Count}).");

            foreach (var option in value)
                ValidateNonEmpty(option, nameof(Options), TextMaxLength);

            return value;
        }

        private static IReadOnlyList<QuizScoreboardEntry> ValidateScoreboard(IReadOnlyList<QuizScoreboardEntry> value)
        {
            if (value is null)
                return value!;

            if (value.Count > ScoreboardMaxCount)
                throw new QuizChannelFeederMessageValidationException(nameof(Scoreboard), $"must not contain more than {ScoreboardMaxCount} entries (had {value.Count}).");

            foreach (var entry in value)
                ValidateNonEmpty(entry.PlayerName, nameof(Scoreboard), TextMaxLength);

            return value;
        }
    }
}
