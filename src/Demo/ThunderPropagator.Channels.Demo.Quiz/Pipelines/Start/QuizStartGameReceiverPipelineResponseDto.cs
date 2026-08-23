using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.Channels.Demo.Quiz.Pipelines.Start
{
    public
#if !DEBUG
        sealed
#endif
        class QuizStartGameReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required QuizStartOutcome Outcome { get; init; }
    }
}
