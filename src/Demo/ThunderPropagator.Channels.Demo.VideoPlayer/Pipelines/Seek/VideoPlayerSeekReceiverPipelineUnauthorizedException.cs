using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Seek
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerSeekReceiverPipeline"/> (see #227) when the calling connection
    /// is not this session's current host. See <c>VideoPlaybackSession.IsHost</c>'s own remarks for
    /// #231's deterministic host-ownership design.
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
