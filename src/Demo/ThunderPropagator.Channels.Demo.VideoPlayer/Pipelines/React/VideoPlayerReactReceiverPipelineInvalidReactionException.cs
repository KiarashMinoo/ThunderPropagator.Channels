using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.React
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerReactReceiverPipeline"/> (see #229) when the submitted reaction is
    /// not currently allowed (<c>ReactionRejectionReason.Unknown</c> — either never a real reaction type,
    /// or one that has since been disabled by removing it from the session's own allowed set) or exceeds
    /// the maximum reaction-name length (<c>ReactionRejectionReason.TooLong</c>). Both map to the same
    /// exception/status: there is no security reason to distinguish them for the caller the way #228's
    /// playlist rejection deliberately hides "unknown vs. disabled," but they are still the same kind of
    /// client-input problem either way, so one 400 response covers both.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerReactReceiverPipelineInvalidReactionException(string message)
        : HttpRequestException(message, null, HttpStatusCode.BadRequest)
    {
    }
}
