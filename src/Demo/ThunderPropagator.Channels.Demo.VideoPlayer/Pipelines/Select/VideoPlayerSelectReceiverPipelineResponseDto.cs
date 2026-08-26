using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Select
{
    /// <summary>Wire response for <see cref="VideoPlayerSelectReceiverPipeline"/> (see #228) — the session's final state once this Select call resolved.</summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSelectReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        /// <summary>The selected video's client-safe id — echoes the request's own <see cref="VideoPlayerSelectReceiverPipelineRequestDto.VideoId"/>.</summary>
        public required string VideoId { get; init; }

        /// <summary>The selected video's human-readable title.</summary>
        public required string Title { get; init; }

        /// <summary>This session's lifecycle state after this call — <see cref="PlayState.Playing"/> on success.</summary>
        public required PlayState State { get; init; }

        /// <summary>This session's stream epoch after this call.</summary>
        public required int Epoch { get; init; }
    }
}
