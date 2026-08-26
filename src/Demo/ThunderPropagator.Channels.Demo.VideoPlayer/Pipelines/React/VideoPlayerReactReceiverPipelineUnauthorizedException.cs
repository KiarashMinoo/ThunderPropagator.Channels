using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.React
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerReactReceiverPipeline"/> (see #229) when the calling connection is
    /// not a subscribed viewer of this channel's session — #229's own scope, "Validate viewer/session
    /// membership." Unlike every other <c>Video/*</c> pipeline's own authorization, this checks
    /// <c>VideoPlaybackSession.IsSubscribed</c>, not host status — any subscribed viewer may react.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerReactReceiverPipelineUnauthorizedException()
        : HttpRequestException("This connection is not subscribed to this session.", null, HttpStatusCode.Forbidden)
    {
    }
}
