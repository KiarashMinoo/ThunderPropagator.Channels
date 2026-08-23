namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// One server-decoded, independently renderable video frame — ordering, timing, dimensions, and
    /// encoding alongside its already-compressed pixel data. Transported as a length-prefixed binary
    /// packet via <see cref="VideoFramePacketSerializer"/>, entirely separate from this channel's JSON
    /// state message: #214's own AC, "Payload bytes are transported as binary, never Base64-in-JSON."
    /// </summary>
    /// <remarks>
    /// <b>Buffer ownership and lifetime:</b> once constructed, a <see cref="VideoFramePacket"/> owns
    /// <see cref="Payload"/> — nothing here ever mutates it, and a packet built by
    /// <see cref="VideoFramePacketSerializer.Read(ReadOnlySpan{byte},out int)"/> holds its own private
    /// copy of the encoded bytes (copied out of the network buffer it was decoded from), so it remains
    /// safe to hold onto and hand off to multiple asynchronous subscribers after that network buffer has
    /// been reused or returned to a pool. A packet built directly by a caller (e.g. a decoder handing
    /// off freshly-encoded bytes) is expected to hand over a buffer it will not mutate afterward — this
    /// type never defensively copies on construction, only <see cref="VideoFramePacketSerializer.Read(ReadOnlySpan{byte},out int)"/>
    /// does, so encoding a packet for the wire (<see cref="VideoFramePacketSerializer.Write(VideoFramePacket,Span{byte})"/>)
    /// never pays for a redundant copy on top of whatever the caller already produced.
    /// </remarks>
    public sealed record VideoFramePacket
    {
        /// <summary>Maximum allowed length of <see cref="SessionId"/>, in UTF-16 characters.</summary>
        public const int SessionIdMaxLength = 128;

        /// <summary>Maximum allowed value of <see cref="Width"/>/<see cref="Height"/> — an 8K ceiling, far beyond any real source.</summary>
        public const int MaxDimension = 7680;

        /// <summary>Maximum allowed length of <see cref="Payload"/>, in bytes — a 16 MiB ceiling for one encoded frame.</summary>
        public const int MaxPayloadLength = 16 * 1024 * 1024;

        /// <summary>Identifies which playback session this frame belongs to.</summary>
        public required string SessionId { get; init; }

        /// <summary>
        /// The session's current stream epoch. Incremented by a seek or source change so stale,
        /// already-in-flight frames from before that change can be recognized and dropped rather than
        /// rendered out of order — see the parent epic's own remarks on epoch-aware invalidation.
        /// </summary>
        public required int Epoch { get; init; }

        /// <summary>0-based, monotonically increasing position of this frame within its <see cref="Epoch"/>.</summary>
        public required long FrameNumber { get; init; }

        /// <summary>This frame's presentation timestamp (PTS) — when it should be displayed, relative to the start of its <see cref="Epoch"/>.</summary>
        public required TimeSpan PresentationTimestamp { get; init; }

        /// <summary>How long this frame should remain on screen before the next one is due.</summary>
        public required TimeSpan Duration { get; init; }

        /// <summary>The wall-clock-relative time the server actually scheduled this frame for display, distinct from <see cref="PresentationTimestamp"/> when server-side pacing has drifted from the source's own timestamps.</summary>
        public required TimeSpan DisplayTime { get; init; }

        /// <summary>Frame width in pixels. Must be strictly positive and at most <see cref="MaxDimension"/>.</summary>
        public required int Width { get; init; }

        /// <summary>Frame height in pixels. Must be strictly positive and at most <see cref="MaxDimension"/>.</summary>
        public required int Height { get; init; }

        /// <summary>Which codec <see cref="Payload"/> is compressed with.</summary>
        public required VideoFramePacketEncoding Encoding { get; init; }

        /// <summary>The already-encoded frame bytes. Must be non-empty and at most <see cref="MaxPayloadLength"/> — see this type's own remarks on ownership.</summary>
        public required ReadOnlyMemory<byte> Payload { get; init; }

        // Records generate member-wise Equals/GetHashCode by default, but ReadOnlyMemory<byte>'s own
        // Equals compares "same underlying memory, same start/length" rather than byte content — after
        // a round trip through VideoFramePacketSerializer, Payload is a freshly-copied array with equal
        // bytes but a different identity, so the compiler-generated Equals would (wrongly) report two
        // otherwise-identical packets as different. Overridden here so content, not identity, decides
        // equality — #214's own AC: "All required fields round-trip losslessly."
        /// <inheritdoc/>
        public bool Equals(VideoFramePacket? other) =>
            other is not null
            && SessionId == other.SessionId
            && Epoch == other.Epoch
            && FrameNumber == other.FrameNumber
            && PresentationTimestamp == other.PresentationTimestamp
            && Duration == other.Duration
            && DisplayTime == other.DisplayTime
            && Width == other.Width
            && Height == other.Height
            && Encoding == other.Encoding
            && Payload.Span.SequenceEqual(other.Payload.Span);

        // Deliberately omits Payload's own content from the hash — hashing every byte of up to
        // MaxPayloadLength on every dictionary insert/lookup would be needlessly expensive for a value
        // used mainly in equality assertions, not as a high-frequency key. Including just its length
        // keeps the Equals/GetHashCode contract intact (equal instances always hash the same) without
        // that cost; it does not need to hash everything Equals compares, only never disagree with it.
        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(SessionId, Epoch, FrameNumber, PresentationTimestamp, HashCode.Combine(Duration, DisplayTime, Width, Height, Encoding, Payload.Length));
    }
}
