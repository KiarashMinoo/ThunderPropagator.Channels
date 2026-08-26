using ThunderPropagator.Application.Collections;
using ThunderPropagator.BuildingBlocks.Application.Collections;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Seek
{
    /// <summary>Wire request for <see cref="VideoPlayerSeekReceiverPipeline"/> (see #227) — the requested playback position.</summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSeekReceiverPipelineRequestDto : BindingDictionary<string, object>, IRequestContentFormCollection
    {
        /// <summary>The requested position, in microseconds — same convention as <see cref="VideoPlayerChannelFeederMessage.MediaPosition"/>. Clamped by <see cref="VideoPlayerSeekReceiverPipeline"/> before use; a negative or out-of-range value is not rejected, only clamped.</summary>
        public required long PositionMicroseconds
        {
            get => (long)this[nameof(PositionMicroseconds)];
            set => this[nameof(PositionMicroseconds)] = value;
        }
    }
}
