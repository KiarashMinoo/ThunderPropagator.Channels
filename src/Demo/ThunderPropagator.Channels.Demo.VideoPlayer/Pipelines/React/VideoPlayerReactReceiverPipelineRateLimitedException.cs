using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.React
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerReactReceiverPipeline"/> (see #229) when the calling viewer has
    /// already recorded the configured maximum number of reactions within the trailing reaction window
    /// (<c>ReactionRejectionReason.RateLimited</c>). <see cref="HttpStatusCode.TooManyRequests"/> (429)
    /// is the one status code that exists specifically for this, unlike the generic 400 the other two
    /// rejection reasons share.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerReactReceiverPipelineRateLimitedException()
        : HttpRequestException("Too many reactions from this connection — try again shortly.", null, HttpStatusCode.TooManyRequests)
    {
    }
}
