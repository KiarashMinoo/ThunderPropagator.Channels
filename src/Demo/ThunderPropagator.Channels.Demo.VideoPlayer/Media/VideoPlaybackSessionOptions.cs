namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
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
    }
}
