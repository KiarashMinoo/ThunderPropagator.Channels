using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Pause
{
    /// <summary>Wire response for <see cref="VideoPlayerPauseReceiverPipeline"/> (see #226) — the one authoritative Paused snapshot this call recorded, including the retained current-frame identity.</summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerPauseReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        /// <summary>This session's lifecycle state after this call — always <see cref="PlayState.Paused"/> on success.</summary>
        public required PlayState State { get; init; }

        /// <summary>This session's stream epoch as of the retained frame.</summary>
        public required int Epoch { get; init; }

        /// <summary>0-based number of the retained frame within <see cref="Epoch"/>.</summary>
        public required long FrameNumber { get; init; }

        /// <summary>The retained frame's own playback position, in microseconds — same convention as <see cref="VideoPlayerChannelFeederMessage.MediaPosition"/>.</summary>
        public required long MediaPosition { get; init; }

        /// <summary>The retained frame's own synchronization time, in microseconds — same convention as <see cref="VideoPlayerChannelFeederMessage.SyncTime"/>.</summary>
        public required long SyncTime { get; init; }
    }
}
