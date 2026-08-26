using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session
{
    /// <summary>Tuning knobs for one <see cref="VideoPlaybackSession"/> — #220's own scope.</summary>
    public sealed record VideoPlaybackSessionOptions
    {
        /// <summary>Passed as <see cref="DecodedFrameBuffer"/>'s own capacity for every generation this session opens.</summary>
        public int DecodeBufferCapacity { get; init; } = 8;

        /// <summary>Passed as each viewer's own <see cref="SubscriberFrameQueue{T}"/> capacity.</summary>
        public int SubscriberQueueCapacity { get; init; } = 8;

        /// <summary>Passed to every <see cref="FramePacer"/> this session creates. Must be strictly positive.</summary>
        public double PlaybackRate { get; init; } = 1.0;

        /// <summary>Which codec published <see cref="VideoFramePacket"/>s are encoded with by the default encoder (see <see cref="VideoPlaybackSession"/>'s own <c>encodeFrame</c> constructor parameter).</summary>
        public VideoFramePacketEncoding Encoding { get; init; } = VideoFramePacketEncoding.Jpeg;

        /// <summary>Quality passed to <see cref="VideoFrameEncoder.Encode"/> by the default encoder.</summary>
        public int Quality { get; init; } = 80;

        /// <summary>How often the publish loop re-checks for a due frame while none is currently due.</summary>
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(5);

        /// <summary>
        /// Whether this session activates an audio pipeline at all — #224's own AC, "Muted/video-only
        /// sessions run without audio resources." <see langword="false"/> means no
        /// <see cref="IAudioFrameSource"/> is ever opened or <see cref="AudioFrameEncoder"/> ever
        /// constructed, regardless of whether the selected video actually has an audio track. Default:
        /// <see langword="true"/> — audio activates automatically whenever the selected video has one
        /// (see <see cref="VideoPlaybackSession"/>'s own <c>audioSourceFactory</c> constructor parameter,
        /// which must also be supplied for audio to ever actually activate).
        /// </summary>
        public bool EnableAudio { get; init; } = true;

        /// <summary>Passed as <see cref="DecodedAudioBuffer"/>'s own capacity for every generation this session opens.</summary>
        public int AudioDecodeBufferCapacity { get; init; } = 16;

        /// <summary>Passed as each viewer's own audio <see cref="SubscriberFrameQueue{T}"/> capacity.</summary>
        public int AudioSubscriberQueueCapacity { get; init; } = 16;

        /// <summary>Target bitrate, in bits per second, passed to the default <see cref="AudioFrameEncoder"/> — suitable for either codec.</summary>
        public int AudioBitRate { get; init; } = 64_000;

        /// <summary>
        /// Which codec a session encodes audio to. <see langword="null"/> (the default) auto-detects: a
        /// selected source whose own audio track is already AAC (<see cref="AudioStreamInfo.SourceCodecName"/>)
        /// encodes to AAC too — avoiding an unnecessary lossy Opus transcode of already-AAC content and
        /// favoring the broader legacy-browser/Safari compatibility AAC offers when the content was
        /// already produced with that compatibility in mind — and every other source encodes to
        /// <see cref="AudioFramePacketEncoding.Opus"/>, the lower-latency, more broadly modern-browser-
        /// native default. Set explicitly to force one codec for every session regardless of source.
        /// See <see cref="VideoPlaybackSession.AudioEncoding"/> for what a given session actually chose.
        /// </summary>
        public AudioFramePacketEncoding? AudioEncoding { get; init; }
    }
}
