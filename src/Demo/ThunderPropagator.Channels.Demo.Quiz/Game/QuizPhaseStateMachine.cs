using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions;

namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// Enforces the quiz game's lifecycle: <see cref="QuizPhase.Lobby"/> →
    /// <see cref="QuizPhase.Question"/> → <see cref="QuizPhase.Revealing"/> →
    /// <see cref="QuizPhase.Scoreboard"/> → next <see cref="QuizPhase.Question"/> or
    /// <see cref="QuizPhase.GameOver"/>. One instance represents one game session's phase; every
    /// transition method checks the current phase and mutates it under the same lock, so two
    /// concurrent callers (e.g. a host's explicit action racing the game loop's own timeout) can never
    /// both succeed in advancing the same session — exactly one wins, the other throws
    /// <see cref="InvalidQuizPhaseTransitionException"/> having left the phase exactly as the winner
    /// left it, never partially applied.
    ///
    /// This type owns only the phase itself — it has no notion of players, questions remaining, or
    /// who the host is (that's session/membership state #187 owns, and question-bank/round state
    /// #188/#189 own). <see cref="NextQuestion"/> takes the "are there more questions" decision as a
    /// parameter for exactly that reason, rather than tracking round counts itself.
    ///
    /// <see cref="Cancel"/> is the single mechanism for every "the game can't continue as normal"
    /// scenario except a deliberate replay: the empty-lobby case (every player leaves mid-game) and
    /// the host-disconnect case both reduce, from this type's perspective, to "abort back to Lobby" —
    /// a caller (a future pipeline or the game loop) calls Cancel when it detects either condition.
    /// Distinguishing why the game was cancelled (notifying remaining players differently, say) is a
    /// session/pipeline concern layered on top of this state machine, not something the phase itself
    /// needs to know.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizPhaseStateMachine
    {
#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif

        private QuizPhase _phase = QuizPhase.Lobby;

        /// <summary>The current phase. Reading this never blocks on an in-flight transition for longer than that transition's own atomic check-and-set.</summary>
        public QuizPhase CurrentPhase
        {
            get
            {
                lock (_lock)
                {
                    return _phase;
                }
            }
        }

        /// <summary>Lobby → Question: the host starts the game.</summary>
        public QuizPhase StartGame() => TransitionFrom(QuizPhase.Lobby, QuizPhase.Question, nameof(StartGame));

        /// <summary>Question → Revealing: the answer window has closed and the correct answer is shown.</summary>
        public QuizPhase RevealAnswer() => TransitionFrom(QuizPhase.Question, QuizPhase.Revealing, nameof(RevealAnswer));

        /// <summary>Revealing → Scoreboard: standings are shown before deciding what comes next.</summary>
        public QuizPhase ShowScoreboard() => TransitionFrom(QuizPhase.Revealing, QuizPhase.Scoreboard, nameof(ShowScoreboard));

        /// <summary>
        /// Scoreboard → Question or GameOver, decided by <paramref name="hasMoreQuestions"/> — the
        /// caller (which owns the question bank/round count, not this type) supplies whether another
        /// round remains.
        /// </summary>
        public QuizPhase NextQuestion(bool hasMoreQuestions)
        {
            lock (_lock)
            {
                RequirePhase(QuizPhase.Scoreboard, nameof(NextQuestion));
                _phase = hasMoreQuestions ? QuizPhase.Question : QuizPhase.GameOver;
                return _phase;
            }
        }

        /// <summary>GameOver → Lobby: a deliberate replay of a finished game.</summary>
        public QuizPhase Restart() => TransitionFrom(QuizPhase.GameOver, QuizPhase.Lobby, nameof(Restart));

        /// <summary>
        /// Question, Revealing, or Scoreboard → Lobby: aborts a game in progress — see this type's
        /// own remarks for why the empty-lobby and host-disconnect cases both route through here
        /// rather than getting their own methods. Not valid from Lobby (nothing to cancel) or
        /// GameOver (use <see cref="Restart"/> instead).
        /// </summary>
        public QuizPhase Cancel()
        {
            lock (_lock)
            {
                if (_phase is QuizPhase.Lobby or QuizPhase.GameOver)
                    throw new InvalidQuizPhaseTransitionException(_phase, nameof(Cancel));

                _phase = QuizPhase.Lobby;
                return _phase;
            }
        }

        private QuizPhase TransitionFrom(QuizPhase from, QuizPhase to, string transitionName)
        {
            lock (_lock)
            {
                RequirePhase(from, transitionName);
                _phase = to;
                return _phase;
            }
        }

        // Callers must already hold _lock. Throwing before any assignment is what guarantees a
        // rejected transition leaves _phase completely untouched — never partially applied.
        private void RequirePhase(QuizPhase required, string transitionName)
        {
            if (_phase != required)
                throw new InvalidQuizPhaseTransitionException(_phase, transitionName);
        }
    }
}
