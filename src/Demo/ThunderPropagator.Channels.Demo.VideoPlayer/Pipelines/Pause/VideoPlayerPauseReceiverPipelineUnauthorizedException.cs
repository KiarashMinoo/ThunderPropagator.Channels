using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Pause
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerPauseReceiverPipeline"/> (see #226) when the calling connection
    /// is not this session's current host. See <c>VideoPlaybackSession.IsHost</c>'s own remarks for
    /// #231's deterministic host-ownership design.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerPauseReceiverPipelineUnauthorizedException()
        : HttpRequestException("This connection is not this session's host.", null, HttpStatusCode.Forbidden)
    {
    }
}
