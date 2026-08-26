using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Pause
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerPauseReceiverPipeline"/> (see #226) when the calling connection
    /// is not this session's host. See <c>VideoPlaybackSession.TryClaimOrVerifyHost</c>'s own remarks for
    /// the temporary host-ownership model this checks against, pending #231's deterministic design.
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
