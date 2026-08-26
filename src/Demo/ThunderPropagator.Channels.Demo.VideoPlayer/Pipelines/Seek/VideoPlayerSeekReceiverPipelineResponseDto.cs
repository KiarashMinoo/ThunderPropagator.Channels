using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Seek
{
    /// <summary>Wire response for <see cref="VideoPlayerSeekReceiverPipeline"/> (see #227) — the session's own synchronized state right after this Seek call committed.</summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSeekReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        /// <summary>This session's lifecycle state after this call — always <see cref="PlayState.Playing"/> on success, since a committed seek always starts a fresh generation.</summary>
        public required PlayState State { get; init; }

        /// <summary>This session's new stream epoch, incremented exactly once by this call — clients use this to invalidate any locally buffered packet from before the seek.</summary>
        public required int Epoch { get; init; }

        /// <summary>
        /// 0-based number of the retained frame within <see cref="Epoch"/> as of this response. Very
        /// likely 0 with <see cref="VideoPlayerSeekReceiverPipeline"/>'s own remarks on why — the new
        /// generation's decode/publish loops start asynchronously and have not necessarily produced their
        /// first frame yet by the time this call returns.
        /// </summary>
        public required long FrameNumber { get; init; }

        /// <summary>The retained frame's own playback position, in microseconds — same convention as <see cref="VideoPlayerChannelFeederMessage.MediaPosition"/>. See <see cref="FrameNumber"/>'s own remarks on why this is often still 0 immediately after a seek.</summary>
        public required long MediaPosition { get; init; }

        /// <summary>The retained frame's own synchronization time, in microseconds — same convention as <see cref="VideoPlayerChannelFeederMessage.SyncTime"/>. See <see cref="FrameNumber"/>'s own remarks.</summary>
        public required long SyncTime { get; init; }
    }
}
