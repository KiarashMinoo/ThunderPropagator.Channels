using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Select
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerSelectReceiverPipeline"/> (see #228) when
    /// <c>VideoPlaybackSession.SelectAsync</c> fails to open an otherwise-approved source — #228's own
    /// AC, "Source failures do not disclose server filesystem, credentials, or private URLs." The
    /// original exception (typically a <c>VideoFrameSourceException</c>) is deliberately never surfaced
    /// here, including its own message or any inner exception: even though today's decoder error
    /// messages only embed a generic FFmpeg error description rather than the file path/URL itself, this
    /// pipeline treats any failure as potentially sensitive rather than depending on that staying true.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="HttpStatusCode.BadGateway"/> (502) rather than a generic 500: this server
    /// successfully identified an approved video and then failed trying to open the actual upstream
    /// media resource on the caller's behalf — the same shape of failure 502 already means for a proxy
    /// that couldn't get a good response from what it was fronting, not an error in this pipeline's own
    /// logic.
    /// </remarks>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSelectReceiverPipelineSourceFailedException()
        : HttpRequestException("Failed to open the selected video.", null, HttpStatusCode.BadGateway)
    {
    }
}
