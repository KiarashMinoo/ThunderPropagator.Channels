using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Select
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerSelectReceiverPipeline"/> (see #228) when the calling connection
    /// is not this session's current host. See <c>VideoPlaybackSession.IsHost</c>'s own remarks for
    /// #231's deterministic host-ownership design.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSelectReceiverPipelineUnauthorizedException()
        : HttpRequestException("This connection is not this session's host.", null, HttpStatusCode.Forbidden)
    {
    }
}
