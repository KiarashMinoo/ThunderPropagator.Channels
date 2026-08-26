using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Seek
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerSeekReceiverPipeline"/> (see #227) when Seek is not valid because
    /// no video has ever been selected for this session (<c>VideoPlaybackSession.CurrentSource</c> is
    /// <see langword="null"/>) — <c>VideoPlaybackSession.SeekAsync</c> itself requires this and throws
    /// <see cref="InvalidOperationException"/> otherwise, which this pipeline never lets leak as an
    /// unhandled 500. Unlike <c>Video/Play</c>/<c>Video/Pause</c>, this is the ONLY state-based rejection
    /// Seek makes — a seek is valid from every <c>PlayState</c> once a source exists, including
    /// <see cref="PlayState.Ended"/>/<see cref="PlayState.Faulted"/> (re-seeking is a legitimate way to
    /// restart at a specific position).
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSeekReceiverPipelineInvalidStateException(string message)
        : HttpRequestException(message, null, HttpStatusCode.Conflict)
    {
    }
}
