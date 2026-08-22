using ThunderPropagator.BuildingBlocks.Application;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    // Issue #185: fields added here to give QuizChannelMetadata's descriptors real properties to
    // describe (see QuizChannelMetadata's own comment on why #185 couldn't wait for #186's full
    // serialization contract). #186 owns hardening this further as needed once the game loop (#189)
    // and scoring (#190) exist to actually populate it end to end.
    internal
#if !DEBUG
        sealed
#endif
        class QuizChannelFeederMessage : FeederMessage
    {
        /// <summary>Identifies which game session this message belongs to — the only subscribing key.</summary>
        public string GameId
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
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
            set => SetValue(value);
        }

        /// <summary>The current question's answer choices. Empty in <see cref="QuizPhase.Lobby"/> and <see cref="QuizPhase.GameOver"/>.</summary>
        public IReadOnlyList<string> Options
        {
            get => GetValueOrNull<IReadOnlyList<string>>() ?? [];
            set => SetValue(value);
        }

        /// <summary>Seconds remaining in the current <see cref="QuizPhase.Question"/> countdown. Meaningless (0) outside that phase.</summary>
        public int TimeRemaining
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        /// <summary>0-based index of the current question within the game.</summary>
        public int QuestionIndex
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        /// <summary>Total number of questions in the game.</summary>
        public int TotalQuestions
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        /// <summary>Current standings. Empty until at least one question has been scored.</summary>
        public IReadOnlyList<QuizScoreboardEntry> Scoreboard
        {
            get => GetValueOrNull<IReadOnlyList<QuizScoreboardEntry>>() ?? [];
            set => SetValue(value);
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
            set => SetValue(value);
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
            set => SetValue(value);
        }
    }
}
