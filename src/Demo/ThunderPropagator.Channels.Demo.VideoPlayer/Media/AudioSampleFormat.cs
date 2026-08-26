namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// The raw sample layout of a <see cref="DecodedAudioFrame"/>'s <see cref="DecodedAudioFrame.Data"/>
    /// — the format a decoder hands off, before <see cref="AudioFrameEncoder"/> compresses it into an
    /// <see cref="AudioFramePacketEncoding"/> for the wire. Not itself a wire format. Mirrors
    /// <see cref="VideoPixelFormat"/>'s own role for the video side.
    /// </summary>
    public enum AudioSampleFormat
    {
        /// <summary>32-bit IEEE float samples, channel-interleaved (e.g. LRLRLR for stereo) — Opus's own preferred input format, avoiding a redundant conversion inside the encoder.</summary>
        Float32Interleaved
    }
}
