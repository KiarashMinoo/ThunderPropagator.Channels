using System.Net;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Select
{
    /// <summary>
    /// Thrown by <see cref="VideoPlayerSelectReceiverPipeline"/> (see #228) when the requested
    /// <c>VideoId</c> either has no matching <see cref="Playlist.VideoPlaylistEntry"/> or matches one
    /// whose <see cref="Playlist.VideoPlaylistEntry.IsEnabled"/> is <see langword="false"/> — #228's own
    /// AC, "Unknown or disabled IDs are rejected," treats both the same way, with the same generic
    /// message either way: distinguishing "this id doesn't exist" from "this id exists but is disabled"
    /// in the response would itself leak information about what the server's playlist contains.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class VideoPlayerSelectReceiverPipelineVideoNotAvailableException()
        : HttpRequestException("This video is not available.", null, HttpStatusCode.NotFound)
    {
    }
}
