namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Extracts audio samples from one media source — the audio-side counterpart to
    /// <see cref="IVideoFrameSource"/>, sharing its own contract verbatim (see that interface's own
    /// remarks): <see cref="ReadFramesAsync"/> called before <see cref="OpenAsync"/> throws
    /// <see cref="InvalidOperationException"/>; an already-cancelled token surfaces
    /// <see cref="OperationCanceledException"/> promptly; frames are yielded in non-decreasing
    /// <see cref="DecodedAudioFrame.PresentationTimestamp"/> order; a source failure is reported as
    /// <see cref="VideoFrameSourceException"/>. A single instance reads exactly one source for its entire
    /// lifetime.
    /// </summary>
    /// <remarks>
    /// Takes a <see cref="VideoSource"/> — the same type <see cref="IVideoFrameSource"/> takes — rather
    /// than a separate audio-specific source type: an audio track and a video track opened for the same
    /// session are always the same underlying file/location (#224's own scope, "the same SessionId,
    /// StreamEpoch, and media timebase" as the video side), never an independently-specified source.
    /// </remarks>
    public interface IAudioFrameSource : IAsyncDisposable
    {
        /// <summary>This source's stream characteristics, or <see langword="null"/> before <see cref="OpenAsync"/> has completed.</summary>
        AudioStreamInfo? StreamInfo { get; }

        /// <summary>Opens <paramref name="source"/> and returns its stream characteristics once known. Must not be called more than once on the same instance.</summary>
        /// <exception cref="VideoFrameSourceException"><paramref name="source"/> has no audio track, or could not be opened.</exception>
        Task<AudioStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default);

        /// <summary>Enumerates audio samples starting at or covering <paramref name="startPosition"/> — see <see cref="IVideoFrameSource.ReadFramesAsync"/>'s own remarks on re-seeking by calling this again.</summary>
        /// <exception cref="InvalidOperationException"><see cref="OpenAsync"/> has not completed successfully yet.</exception>
        /// <exception cref="VideoFrameSourceException">Reading failed for a reason intrinsic to the source.</exception>
        IAsyncEnumerable<DecodedAudioFrame> ReadFramesAsync(TimeSpan startPosition, CancellationToken cancellationToken = default);
    }
}
