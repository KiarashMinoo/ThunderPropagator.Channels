using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// An audio stream's static characteristics, known once <see cref="IAudioFrameSource.OpenAsync"/>
    /// completes. Mirrors <see cref="VideoStreamInfo"/>'s own role for the video side — nothing here is
    /// ever used to reconstruct a frame's own timing; every <see cref="DecodedAudioFrame"/> carries its
    /// own <see cref="DecodedAudioFrame.PresentationTimestamp"/>/<see cref="DecodedAudioFrame.Duration"/>
    /// exactly as the source reported them.
    /// </summary>
    public sealed record AudioStreamInfo
    {
        /// <summary>Samples per second every <see cref="DecodedAudioFrame"/> this source yields uses.</summary>
        public required int SampleRate { get; init; }

        /// <summary>Channel count every <see cref="DecodedAudioFrame"/> this source yields uses (1 = mono, 2 = stereo).</summary>
        public required int Channels { get; init; }

        /// <summary>The raw sample layout every <see cref="DecodedAudioFrame"/> this source yields uses.</summary>
        public required AudioSampleFormat SampleFormat { get; init; }

        /// <summary>Total stream duration, or <see cref="TimeSpan.Zero"/> for a source of unknown or indeterminate (e.g. live) length.</summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// The source's own original audio codec name (e.g. <c>"aac"</c>, <c>"mp3"</c>, <c>"opus"</c>,
        /// <c>"vorbis"</c>) before this source ever decoded/resampled it — never a wire
        /// <see cref="AudioFramePacketEncoding"/> itself, purely informational. A
        /// <see cref="VideoPlaybackSession"/> uses this to auto-pick an output encoding when the caller
        /// has not configured one explicitly — see <see cref="VideoPlaybackSessionOptions.AudioEncoding"/>'s
        /// own remarks. Empty if unknown.
        /// </summary>
        public string SourceCodecName { get; init; } = string.Empty;
    }
}
