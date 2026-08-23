namespace ThunderPropagator.Channels.Demo.Quiz.Game.Enums
{
    /// <summary>
    /// What happened to one call to <see cref="QuizGameLoop.SubmitAnswer"/> — the information a future
    /// answer-submission pipeline (#192) needs to build its own unicast acknowledgement, without that
    /// pipeline having to re-derive any of #190's own scoring rules itself.
    /// </summary>
    internal enum QuizAnswerOutcome
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
        Duplicate
    }
}
