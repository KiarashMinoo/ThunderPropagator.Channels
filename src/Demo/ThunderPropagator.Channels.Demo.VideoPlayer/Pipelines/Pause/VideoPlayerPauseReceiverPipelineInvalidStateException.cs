using System.Net;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Pause
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerPauseReceiverPipeline"/> (see #226) when Pause is not valid from
    /// this session's current state: no source has ever been selected (<c>VideoPlaybackSession.CurrentSource</c>
    /// is <see langword="null"/>), the session is still <see cref="PlayState.Loading"/> (nothing has been
    /// published yet, so there is no "current frame" to freeze), or it has already
    /// <see cref="PlayState.Ended"/> or <see cref="PlayState.Faulted"/> — none of those have a stable
    /// position/frame for Pause to record, per this ticket's own AC.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerPauseReceiverPipelineInvalidStateException(string message)
        : HttpRequestException(message, null, HttpStatusCode.Conflict)
    {
    }
}
