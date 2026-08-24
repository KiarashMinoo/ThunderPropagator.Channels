namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Extracts frames from one video source — the replaceable abstraction a concrete decoder (e.g. the
    /// FFmpeg-backed implementation #217 adds) implements, and the one a synthetic implementation
    /// satisfies for deterministic tests without any real media or native dependency. A single instance
    /// reads exactly one <see cref="VideoSource"/> for its entire lifetime; opening a different source
    /// means disposing this instance and creating another.
    /// </summary>
    /// <remarks>
    /// Contract every implementation must satisfy (exercised by the reusable contract test suite this
    /// ticket also adds, so a new implementation can verify itself against the same tests):
    /// <list type="bullet">
    /// <item><description><see cref="ReadFramesAsync"/> called before <see cref="OpenAsync"/> throws <see cref="InvalidOperationException"/>.</description></item>
    /// <item><description>An already-cancelled <see cref="CancellationToken"/> passed to either method surfaces <see cref="OperationCanceledException"/> promptly, before any frame is yielded.</description></item>
    /// <item><description>Frames are yielded in non-decreasing <see cref="DecodedVideoFrame.PresentationTimestamp"/> order.</description></item>
    /// <item><description>A source failure (as opposed to cancellation or caller misuse) is reported as <see cref="VideoFrameSourceException"/>.</description></item>
    /// </list>
    /// </remarks>
    public interface IVideoFrameSource : IAsyncDisposable
    {
        /// <summary>This source's stream characteristics, or <see langword="null"/> before <see cref="OpenAsync"/> has completed.</summary>
        VideoStreamInfo? StreamInfo { get; }

        /// <summary>
        /// Opens <paramref name="source"/> and returns its stream characteristics once known. Must not
        /// be called more than once on the same instance.
        /// </summary>
        /// <exception cref="VideoFrameSourceException"><paramref name="source"/> could not be opened.</exception>
        Task<VideoStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerates frames starting at or covering <paramref name="startPosition"/> — zero for the
        /// beginning of the stream, or any other position to seek to it (calling this again, including
        /// mid-enumeration of a previous call, is how a caller re-seeks; an implementation must abandon
        /// any prior enumeration's in-progress decode state cleanly). Every yielded
        /// <see cref="DecodedVideoFrame"/> must eventually be disposed by the caller — see that type's
        /// own remarks on buffer ownership.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="OpenAsync"/> has not completed successfully yet.</exception>
        /// <exception cref="VideoFrameSourceException">Reading failed for a reason intrinsic to the source.</exception>
        IAsyncEnumerable<DecodedVideoFrame> ReadFramesAsync(TimeSpan startPosition, CancellationToken cancellationToken = default);
    }
}
