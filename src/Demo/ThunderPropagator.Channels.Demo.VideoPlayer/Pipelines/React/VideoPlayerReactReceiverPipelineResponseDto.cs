using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.React
{
    /// <summary>Wire response for <see cref="VideoPlayerReactReceiverPipeline"/> (see #229) — the aggregate reaction counts as of this call, including the just-recorded submission.</summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerReactReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        /// <summary>Current aggregate reaction counts, same shape and window as <see cref="VideoPlayerChannelFeederMessage.Reactions"/>.</summary>
        public required IReadOnlyList<VideoReactionCount> Reactions { get; init; }
    }
}
