using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Play
{
    /// <summary>Wire response for <see cref="VideoPlayerPlayReceiverPipeline"/> (see #225) — the session's own synchronized state right after this Play call resolved.</summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerPlayReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        /// <summary>This session's lifecycle state after this call — always <see cref="PlayState.Playing"/> on success.</summary>
        public required PlayState State { get; init; }

        /// <summary>This session's current stream epoch.</summary>
        public required int Epoch { get; init; }
    }
}
