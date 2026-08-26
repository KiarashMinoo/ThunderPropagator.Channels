using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.React
{
    /// <summary>Wire request for <see cref="VideoPlayerReactReceiverPipeline"/> (see #229) — the reaction to submit.</summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerReactReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        /// <summary>The reaction string to record — validated against the session's own allowed set, length limit, and this viewer's own rate limit by <see cref="Media.Session.ReactionAggregator"/>.</summary>
        public required string Reaction
        {
            get => (string)this[nameof(Reaction)];
            set => this[nameof(Reaction)] = value;
        }
    }
}
