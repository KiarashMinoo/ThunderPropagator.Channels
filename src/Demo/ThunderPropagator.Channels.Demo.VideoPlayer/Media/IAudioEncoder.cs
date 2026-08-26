namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// What <see cref="VideoPlaybackSession"/> needs from an audio encoder — the abstraction
    /// <see cref="AudioFrameEncoder"/> implements for real Opus encoding, and a deterministic test double
    /// can implement instead so a session's own audio wiring is testable without any native FFmpeg
    /// dependency, mirroring how <c>encodeFrame</c> is already injectable for the video side.
    /// </summary>
    public interface IAudioEncoder : IDisposable
    {
        /// <summary>Samples per channel this instance expects <see cref="Encode"/> to eventually accumulate before it can produce output — see <see cref="AudioFrameEncoder.FrameSize"/>'s own remarks.</summary>
        int FrameSize { get; }

        /// <summary>See <see cref="AudioFrameEncoder.Encode"/>.</summary>
        IReadOnlyList<EncodedAudioChunk> Encode(DecodedAudioFrame frame);

        /// <summary>See <see cref="AudioFrameEncoder.Flush"/>.</summary>
        IReadOnlyList<EncodedAudioChunk> Flush();
    }
}
