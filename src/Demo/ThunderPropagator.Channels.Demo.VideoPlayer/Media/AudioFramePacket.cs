namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// One server-encoded chunk of audio — the audio-side counterpart to <see cref="VideoFramePacket"/>,
    /// sharing its own <see cref="SessionId"/>/<see cref="Epoch"/> so a client can correlate audio and
    /// video from the same session and stream epoch — #224's own AC, "Audio and video packets share one
    /// session epoch and synchronized timestamps." Transported as a length-prefixed binary packet via
    /// <see cref="AudioFramePacketSerializer"/>, never Base64-in-JSON — #224's own AC.
    /// </summary>
    /// <remarks>
    /// <b>Buffer ownership and lifetime:</b> identical to <see cref="VideoFramePacket"/>'s own — once
    /// constructed, this type owns <see cref="Payload"/>; a packet built by
    /// <see cref="AudioFramePacketSerializer.Read(ReadOnlySpan{byte},out int)"/> holds its own private
    /// copy, while a packet built directly by a caller is expected to hand over a buffer it will not
    /// mutate afterward.
    /// </remarks>
    public sealed record AudioFramePacket
    {
        /// <summary>Maximum allowed length of <see cref="SessionId"/>, in UTF-16 characters.</summary>
        public const int SessionIdMaxLength = 128;

        /// <summary>Maximum allowed length of <see cref="Payload"/>, in bytes — one encoded Opus packet is always far smaller than this, generously bounding against a corrupt/hostile length.</summary>
        public const int MaxPayloadLength = 64 * 1024;

        /// <summary>Identifies which playback session this audio belongs to — always the same value as the corresponding <see cref="VideoFramePacket.SessionId"/>.</summary>
        public required string SessionId { get; init; }

        /// <summary>This session's current stream epoch — always the same value as the corresponding <see cref="VideoFramePacket.Epoch"/>. See that field's own remarks.</summary>
        public required int Epoch { get; init; }

        /// <summary>0-based, monotonically increasing position of this packet within its <see cref="Epoch"/> — the audio-side counterpart to <see cref="VideoFramePacket.FrameNumber"/>, counted independently of it.</summary>
        public required long PacketNumber { get; init; }

        /// <summary>This packet's presentation timestamp — when it should start playing, relative to the start of its <see cref="Epoch"/>, on the same media timebase <see cref="VideoFramePacket.PresentationTimestamp"/> uses.</summary>
        public required TimeSpan PresentationTimestamp { get; init; }

        /// <summary>How long this packet plays before the next one is due.</summary>
        public required TimeSpan Duration { get; init; }

        /// <summary>The wall-clock-relative time the server actually scheduled this packet for playback — the audio-side counterpart to <see cref="VideoFramePacket.DisplayTime"/>, computed from the same <c>FramePacer</c> so both tracks share one synchronized clock.</summary>
        public required TimeSpan DisplayTime { get; init; }

        /// <summary>Samples per second the encoded <see cref="Payload"/> represents.</summary>
        public required int SampleRate { get; init; }

        /// <summary>Channel count the encoded <see cref="Payload"/> represents.</summary>
        public required int Channels { get; init; }

        /// <summary>Which codec <see cref="Payload"/> is compressed with.</summary>
        public required AudioFramePacketEncoding Encoding { get; init; }

        /// <summary>The already-encoded audio bytes. Must be non-empty and at most <see cref="MaxPayloadLength"/> — see this type's own remarks on ownership.</summary>
        public required ReadOnlyMemory<byte> Payload { get; init; }

        // See VideoFramePacket's own remarks on why Equals/GetHashCode are overridden: ReadOnlyMemory<byte>'s
        // compiler-generated equality compares buffer identity, not content, which would make two
        // otherwise-identical packets compare unequal after a serializer round trip copies Payload into a
        // freshly-allocated array.
        /// <inheritdoc/>
        public bool Equals(AudioFramePacket? other) =>
            other is not null
            && SessionId == other.SessionId
            && Epoch == other.Epoch
            && PacketNumber == other.PacketNumber
            && PresentationTimestamp == other.PresentationTimestamp
            && Duration == other.Duration
            && DisplayTime == other.DisplayTime
            && SampleRate == other.SampleRate
            && Channels == other.Channels
            && Encoding == other.Encoding
            && Payload.Span.SequenceEqual(other.Payload.Span);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(SessionId, Epoch, PacketNumber, PresentationTimestamp, HashCode.Combine(Duration, DisplayTime, SampleRate, Channels, Encoding, Payload.Length));
    }
}
