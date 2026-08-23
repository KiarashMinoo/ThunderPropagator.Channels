using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Demo.Quiz.Pipelines.Answer
{
    public
#if !DEBUG
        sealed
#endif
        class QuizSubmitAnswerReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        /// <summary>The game this answer targets.</summary>
        public required string GameId
        {
            get => (string)this[nameof(GameId)];
            set => this[nameof(GameId)] = value;
        }

        /// <summary>
        /// The question this answer targets, as a token proving which question it was submitted for —
        /// rejected as <see cref="Game.Enums.QuizAnswerOutcome.Stale"/> if the game has since moved on
        /// (or has not yet reached this index).
        /// </summary>
        public required int QuestionIndex
        {
            get => (int)this[nameof(QuestionIndex)];
            set => this[nameof(QuestionIndex)] = value;
        }

        /// <summary>
        /// 0-based index into the question's options — never the option's text itself, so nothing
        /// about what the correct answer looks like is ever implied by the shape of this request.
        /// </summary>
        public required int OptionIndex
        {
            get => (int)this[nameof(OptionIndex)];
            set => this[nameof(OptionIndex)] = value;
        }
    }
}
