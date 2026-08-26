using System.Net;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Play
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerPlayReceiverPipeline"/> (see #225) when Play is not valid from
    /// this session's current state: no source has ever been selected (<c>VideoPlaybackSession.CurrentSource</c>
    /// is <see langword="null"/> — that's <c>Video/Select</c>'s own job, #228, not this pipeline's), or the
    /// session has already <see cref="PlayState.Ended"/> or <see cref="PlayState.Faulted"/> — neither of
    /// which Play can resume from. Matches this ticket's own AC: "Play from a valid Loading/Paused state
    /// transitions once to Playing" — anything else is the "documented conflict" that AC also allows for.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerPlayReceiverPipelineInvalidStateException(string message)
        : HttpRequestException(message, null, HttpStatusCode.Conflict)
    {
    }
}
