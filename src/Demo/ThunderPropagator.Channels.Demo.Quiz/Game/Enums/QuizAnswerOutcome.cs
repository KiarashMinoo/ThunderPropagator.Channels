namespace ThunderPropagator.Channels.Demo.Quiz.Game.Enums
{
    /// <summary>
    /// What happened to one call to <see cref="QuizGameLoop.SubmitAnswer"/> — the information the
    /// answer-submission pipeline (#192, <c>QuizSubmitAnswerReceiverPipeline</c>) needs to build its
    /// own unicast acknowledgement, without that pipeline having to re-derive any of #190's own scoring
    /// rules itself. Public (unlike most of this namespace's types) because it is itself part of that
    /// pipeline's public response contract — see <c>QuizSubmitAnswerReceiverPipelineResponseDto</c>.
    /// </summary>
    public enum QuizAnswerOutcome
    {
        /// <summary>The answer was accepted and matched the question's correct option — scored.</summary>
        Correct,

        /// <summary>The answer was accepted but did not match the question's correct option — scored zero.</summary>
        Incorrect,

        /// <summary>
        /// No question is currently open to answer — either none has started yet, or this one's
        /// answer window already closed (Question phase ended). Scored zero either way; see #190's
        /// own AC ("Late and invalid answers receive no score") for why the two aren't distinguished
        /// further.
        /// </summary>
        WindowClosed,

        /// <summary>
        /// This player already submitted an answer for the current question — #190's own AC on
        /// accepting at most one effective answer per player per question. The first submission is
        /// final and this one is ignored, whether the first was correct or not.
        /// </summary>
        Duplicate,

        /// <summary>
        /// The submission's own question index (#192's own AC: validate "question index/token") does
        /// not match whichever question is actually open right now — an answer meant for a question
        /// the game has already moved on from (or one it has not reached yet). Distinct from
        /// <see cref="WindowClosed"/>, which means no question is open at all right now regardless of
        /// which one the caller thought they were answering.
        /// </summary>
        Stale,

        /// <summary>
        /// The submitted option index does not correspond to any of the current question's options —
        /// #192's own AC: validate "option index". Never reaches <see cref="QuizScoringEngine.SubmitAnswer"/>
        /// at all, so it does not consume this player's one answer for the question the way an accepted
        /// but wrong answer does.
        /// </summary>
        Invalid
    }
}
