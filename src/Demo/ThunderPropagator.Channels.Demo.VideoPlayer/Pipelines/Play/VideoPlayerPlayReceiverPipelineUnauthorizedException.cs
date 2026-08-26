using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Play
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerPlayReceiverPipeline"/> (see #225) when the calling connection
    /// is not this session's current host — either it never subscribed at all, or a different, still
    /// eligible subscriber holds host status. See <c>VideoPlaybackSession.IsHost</c>'s own remarks for
    /// #231's deterministic host-ownership design.
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
