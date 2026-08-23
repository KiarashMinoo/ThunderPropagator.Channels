using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Demo.Quiz.Pipelines.Start
{
    public
#if !DEBUG
        sealed
#endif
        class QuizStartGameReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        /// <summary>
        /// The game to start. Carries no player identity — who is starting it is resolved server-side
        /// from the calling connection's own established host status, never from a value a caller could
        /// supply (#193's own AC: "Authorize the session host from server-side connection state").
        /// </summary>
        public required string GameId
        {
            get => (string)this[nameof(GameId)];
            set => this[nameof(GameId)] = value;
        }
    }
}
