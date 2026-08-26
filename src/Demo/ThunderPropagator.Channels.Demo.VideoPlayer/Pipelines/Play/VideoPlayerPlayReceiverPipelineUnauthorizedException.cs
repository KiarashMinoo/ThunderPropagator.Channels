using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Play
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerPlayReceiverPipeline"/> (see #225) when the calling connection
    /// is not this session's host — either a different connection already claimed host status, or (not
    /// relevant here, since claiming happens as part of this same call) never will. See
    /// <c>VideoPlaybackSession.TryClaimOrVerifyHost</c>'s own remarks for the temporary host-ownership
    /// model this checks against, pending #231's deterministic design.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerPlayReceiverPipelineUnauthorizedException()
        : HttpRequestException("This connection is not this session's host.", null, HttpStatusCode.Forbidden)
    {
    }
}
