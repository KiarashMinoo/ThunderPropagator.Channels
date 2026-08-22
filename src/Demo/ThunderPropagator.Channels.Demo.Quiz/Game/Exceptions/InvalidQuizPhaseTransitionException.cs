using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="QuizPhaseStateMachine"/> when a transition is attempted from a phase it
    /// doesn't apply to (e.g. <c>RevealAnswer</c> while still in <see cref="QuizPhase.Lobby"/>). The
    /// state machine's current phase is left unchanged when this is thrown — see
    /// <see cref="QuizPhaseStateMachine"/>'s own comments for why every transition method checks its
    /// precondition before mutating anything.
    /// </summary>
    public sealed class InvalidQuizPhaseTransitionException(QuizPhase currentPhase, string attemptedTransition)
        : Exception($"Cannot perform '{attemptedTransition}' while in phase '{currentPhase}'.")
    {
        public QuizPhase CurrentPhase { get; } = currentPhase;
    }
}
