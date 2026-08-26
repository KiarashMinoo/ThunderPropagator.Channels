using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Seek
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerSeekReceiverPipeline"/> (see #227) when the calling connection
    /// is not this session's host. See <c>VideoPlaybackSession.TryClaimOrVerifyHost</c>'s own remarks for
    /// the temporary host-ownership model this checks against, pending #231's deterministic design.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSeekReceiverPipelineUnauthorizedException()
        : HttpRequestException("This connection is not this session's host.", null, HttpStatusCode.Forbidden)
    {
    }
}
