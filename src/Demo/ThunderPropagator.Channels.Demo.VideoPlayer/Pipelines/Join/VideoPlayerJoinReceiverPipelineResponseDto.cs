using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Join
{
    /// <summary>Wire response for <see cref="VideoPlayerJoinReceiverPipeline"/> (see #230) — the atomic bootstrap snapshot this join captured.</summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerJoinReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        /// <summary>This session's lifecycle state as of this join's own atomic snapshot.</summary>
        public required PlayState State { get; init; }

        /// <summary>This session's stream epoch as of this join's own atomic snapshot.</summary>
        public required int Epoch { get; init; }

        /// <summary><see langword="true"/> if a frame had already been published and was unicast into this viewer's own queue; <see langword="false"/> if nothing has published yet (e.g. joining immediately after <c>Video/Select</c>, before decode has produced a frame).</summary>
        public required bool HasBootstrapFrame { get; init; }

        /// <summary>0-based number of the bootstrap frame within <see cref="Epoch"/> — 0 if <see cref="HasBootstrapFrame"/> is <see langword="false"/>.</summary>
        public required long FrameNumber { get; init; }

        /// <summary>The bootstrap frame's own playback position, in microseconds — same convention as <see cref="VideoPlayerChannelFeederMessage.MediaPosition"/>.</summary>
        public required long MediaPosition { get; init; }

        /// <summary>The bootstrap frame's own synchronization time, in microseconds — same convention as <see cref="VideoPlayerChannelFeederMessage.SyncTime"/>.</summary>
        public required long SyncTime { get; init; }

        /// <summary>
        /// <see langword="true"/> if this same connection was already subscribed at the moment this
        /// join arrived — a client rejoining without a clean prior unsubscribe, or deliberately
        /// re-requesting a fresh bootstrap — <see langword="false"/> for a genuinely new viewer. #230's
        /// own scope, "Mark reconnect snapshots so a client can reset its local session state."
        /// </summary>
        public required bool IsReconnect { get; init; }
    }
}
