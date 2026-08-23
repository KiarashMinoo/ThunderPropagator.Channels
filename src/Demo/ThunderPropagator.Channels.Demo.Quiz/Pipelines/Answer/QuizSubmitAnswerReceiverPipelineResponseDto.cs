using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.Channels.Demo.Quiz.Pipelines.Answer
{
    /// <summary>
    /// Deliberately carries only <see cref="Outcome"/> — never the question's correct answer or
    /// option, before or after reveal — #192's own AC: "Correct-answer data is not leaked in the
    /// acknowledgement." A submitter can see their own outcome (including whether they were
    /// <see cref="QuizAnswerOutcome.Correct"/>) since that reveals nothing they did not already choose
    /// themselves; it never reveals what anyone else answered, or what the right answer actually is.
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class QuizSubmitAnswerReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required QuizAnswerOutcome Outcome { get; init; }
    }
}
