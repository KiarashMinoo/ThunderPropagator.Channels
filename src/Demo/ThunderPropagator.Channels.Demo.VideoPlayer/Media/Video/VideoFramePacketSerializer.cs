using System.Buffers.Binary;
using System.Text;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// Encodes/decodes a <see cref="VideoFramePacket"/> to/from a compact, length-prefixed binary
    /// layout — #214's own "binary packet serializer/framing format separate from channel state JSON."
    /// </summary>
    /// <remarks>
    /// Wire layout (all multi-byte integers little-endian):
    /// <code>
    /// [0]        FormatVersion   (byte)
    /// [1]        Encoding        (byte)
    /// [2..6)     Epoch           (int32)
    /// [6..10)    Width           (int32)
    /// [10..14)   Height          (int32)
    /// [14..22)   FrameNumber     (int64)
    /// [22..30)   PresentationTimestamp.Ticks (int64)
    /// [30..38)   Duration.Ticks  (int64)
    /// [38..46)   DisplayTime.Ticks (int64)
    /// [46..50)   SessionId byte length (int32)
    /// [50..50+S) SessionId UTF-8 bytes
    /// [50+S..+4) Payload byte length (int32)
    /// [.. +P)    Payload bytes
    /// </code>
    /// <see cref="CurrentFormatVersion"/> is checked on every read, so a future incompatible layout
    /// change can bump it and have old readers reject the new format cleanly instead of misparsing it —
    /// #214's own "versioning strategy" AC.
    /// </remarks>
    public static class VideoFramePacketSerializer
    {
        /// <summary>The only wire format version this serializer currently reads or writes.</summary>
        public const byte CurrentFormatVersion = 1;

        private const int FormatVersionOffset = 0;
        private const int EncodingOffset = 1;
        private const int EpochOffset = 2;
        private const int WidthOffset = 6;
        private const int HeightOffset = 10;
        private const int FrameNumberOffset = 14;
        private const int PresentationTimestampOffset = 22;
        private const int DurationOffset = 30;
        private const int DisplayTimeOffset = 38;
        private const int SessionIdLengthOffset = 46;

        /// <summary>Size, in bytes, of every fixed-position field before the variable-length <see cref="VideoFramePacket.SessionId"/>.</summary>
        private const int FixedHeaderSize = 50;

        /// <summary>The exact number of bytes <see cref="Write(VideoFramePacket,Span{byte})"/> will write for <paramref name="packet"/>.</summary>
        public static int GetSize(VideoFramePacket packet)
        {
            ArgumentNullException.ThrowIfNull(packet);

            return FixedHeaderSize + Encoding.UTF8.GetByteCount(packet.SessionId) + sizeof(int) + packet.Payload.Length;
        }

        /// <summary>
        /// Serializes <paramref name="packet"/> into a freshly allocated array sized exactly to fit it.
        /// Convenience wrapper around <see cref="Write(VideoFramePacket,Span{byte})"/> for callers that
        /// do not already own a destination buffer.
        /// </summary>
        public static byte[] Write(VideoFramePacket packet)
        {
            var buffer = new byte[GetSize(packet)];
            Write(packet, buffer);
            return buffer;
        }

        /// <summary>
        /// Serializes <paramref name="packet"/> into <paramref name="destination"/>, which must be at
        /// least <see cref="GetSize"/> bytes. Copies <see cref="VideoFramePacket.Payload"/>'s bytes into
        /// <paramref name="destination"/> but never copies or reallocates it internally otherwise, so
        /// writing into a pre-sized, reused buffer allocates nothing beyond that one copy.
        /// </summary>
        /// <returns>The number of bytes actually written — always equal to <see cref="GetSize"/>.</returns>
        public static int Write(VideoFramePacket packet, Span<byte> destination)
        {
            ArgumentNullException.ThrowIfNull(packet);
            Validate(packet);

            var sessionIdByteCount = Encoding.UTF8.GetByteCount(packet.SessionId);
            var totalSize = FixedHeaderSize + sessionIdByteCount + sizeof(int) + packet.Payload.Length;

            if (destination.Length < totalSize)
                throw new ArgumentException($"Destination span ({destination.Length} byte(s)) is too small for this packet ({totalSize} byte(s)).", nameof(destination));

            destination[FormatVersionOffset] = CurrentFormatVersion;
            destination[EncodingOffset] = (byte)packet.Encoding;
            BinaryPrimitives.WriteInt32LittleEndian(destination[EpochOffset..], packet.Epoch);
            BinaryPrimitives.WriteInt32LittleEndian(destination[WidthOffset..], packet.Width);
            BinaryPrimitives.WriteInt32LittleEndian(destination[HeightOffset..], packet.Height);
            BinaryPrimitives.WriteInt64LittleEndian(destination[FrameNumberOffset..], packet.FrameNumber);
            BinaryPrimitives.WriteInt64LittleEndian(destination[PresentationTimestampOffset..], packet.PresentationTimestamp.Ticks);
            BinaryPrimitives.WriteInt64LittleEndian(destination[DurationOffset..], packet.Duration.Ticks);
            BinaryPrimitives.WriteInt64LittleEndian(destination[DisplayTimeOffset..], packet.DisplayTime.Ticks);
            BinaryPrimitives.WriteInt32LittleEndian(destination[SessionIdLengthOffset..], sessionIdByteCount);

            var cursor = FixedHeaderSize;
            Encoding.UTF8.GetBytes(packet.SessionId, destination.Slice(cursor, sessionIdByteCount));
            cursor += sessionIdByteCount;

            BinaryPrimitives.WriteInt32LittleEndian(destination[cursor..], packet.Payload.Length);
            cursor += sizeof(int);

            packet.Payload.Span.CopyTo(destination.Slice(cursor, packet.Payload.Length));

            return totalSize;
        }

        /// <summary>
        /// Deserializes exactly one packet from <paramref name="source"/>, which must contain nothing
        /// but that one packet — throws <see cref="VideoFramePacketValidationException"/> if any byte
        /// remains afterward. Use <see cref="Read(ReadOnlySpan{byte},out int)"/> instead when
        /// <paramref name="source"/> may contain more than one packet back-to-back.
        /// </summary>
        public static VideoFramePacket Read(ReadOnlySpan<byte> source)
        {
            var packet = Read(source, out var bytesConsumed);

            if (bytesConsumed != source.Length)
                throw new VideoFramePacketValidationException(nameof(source), $"has {source.Length - bytesConsumed} unexpected trailing byte(s) after one complete packet.");

            return packet;
        }

        /// <summary>
        /// Deserializes one packet from the start of <paramref name="source"/>, reporting how many
        /// bytes it actually consumed via <paramref name="bytesConsumed"/> so a caller framing multiple
        /// packets over a stream can advance past exactly this one. The returned packet's
        /// <see cref="VideoFramePacket.Payload"/> is always a private copy of the corresponding bytes in
        /// <paramref name="source"/> — see <see cref="VideoFramePacket"/>'s own remarks on why a decode
        /// never aliases the source buffer.
        /// </summary>
        /// <exception cref="VideoFramePacketValidationException">
        /// <paramref name="source"/> is truncated, declares an unsupported format version or encoding,
        /// or declares a session-id/payload length that is invalid or does not fit in the remaining
        /// buffer — #214's own AC: "Malformed lengths, unsupported encodings, and oversized payloads are
        /// rejected."
        /// </exception>
        public static VideoFramePacket Read(ReadOnlySpan<byte> source, out int bytesConsumed)
        {
            if (source.Length < FixedHeaderSize)
                throw new VideoFramePacketValidationException(nameof(source), $"must contain at least {FixedHeaderSize} fixed header byte(s) (had {source.Length}).");

            var formatVersion = source[FormatVersionOffset];
            if (formatVersion != CurrentFormatVersion)
                throw new VideoFramePacketValidationException(nameof(formatVersion), $"is {formatVersion}, but only {CurrentFormatVersion} is supported.");

            var encodingValue = source[EncodingOffset];
            var encoding = (VideoFramePacketEncoding)encodingValue;
            if (!Enum.IsDefined(encoding))
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.Encoding), $"value {encodingValue} is not a supported encoding.");

            var epoch = BinaryPrimitives.ReadInt32LittleEndian(source[EpochOffset..]);
            var width = BinaryPrimitives.ReadInt32LittleEndian(source[WidthOffset..]);
            var height = BinaryPrimitives.ReadInt32LittleEndian(source[HeightOffset..]);
            ValidateDimensions(width, height);

            var frameNumber = BinaryPrimitives.ReadInt64LittleEndian(source[FrameNumberOffset..]);
            var presentationTimestamp = TimeSpan.FromTicks(BinaryPrimitives.ReadInt64LittleEndian(source[PresentationTimestampOffset..]));
            var duration = TimeSpan.FromTicks(BinaryPrimitives.ReadInt64LittleEndian(source[DurationOffset..]));
            var displayTime = TimeSpan.FromTicks(BinaryPrimitives.ReadInt64LittleEndian(source[DisplayTimeOffset..]));

            var sessionIdByteLength = BinaryPrimitives.ReadInt32LittleEndian(source[SessionIdLengthOffset..]);
            // UTF-8 encodes any character in at most 4 bytes, so this bound rejects an absurd/corrupted
            // declared length before attempting to slice the buffer with it, even before the exact
            // character-count check below runs.
            if (sessionIdByteLength <= 0 || sessionIdByteLength > VideoFramePacket.SessionIdMaxLength * 4)
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.SessionId), $"declared byte length ({sessionIdByteLength}) is invalid.");

            var cursor = FixedHeaderSize;
            if (source.Length < cursor + sessionIdByteLength + sizeof(int))
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.SessionId), "declared length extends beyond the available buffer.");

            var sessionId = Encoding.UTF8.GetString(source.Slice(cursor, sessionIdByteLength));
            if (sessionId.Length > VideoFramePacket.SessionIdMaxLength)
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.SessionId), $"must not exceed {VideoFramePacket.SessionIdMaxLength} character(s) (was {sessionId.Length}).");
            cursor += sessionIdByteLength;

            var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(source[cursor..]);
            cursor += sizeof(int);

            if (payloadLength <= 0 || payloadLength > VideoFramePacket.MaxPayloadLength)
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.Payload), $"declared length ({payloadLength}) must be between 1 and {VideoFramePacket.MaxPayloadLength} byte(s).");

            if (source.Length < cursor + payloadLength)
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.Payload), "declared length extends beyond the available buffer.");

            var payload = source.Slice(cursor, payloadLength).ToArray();
            cursor += payloadLength;

            bytesConsumed = cursor;

            return new VideoFramePacket
            {
                SessionId = sessionId,
                Epoch = epoch,
                FrameNumber = frameNumber,
                PresentationTimestamp = presentationTimestamp,
                Duration = duration,
                DisplayTime = displayTime,
                Width = width,
                Height = height,
                Encoding = encoding,
                Payload = payload
            };
        }

        private static void Validate(VideoFramePacket packet)
        {
            if (string.IsNullOrWhiteSpace(packet.SessionId))
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.SessionId), "must not be null, empty, or whitespace-only.");

            if (packet.SessionId.Length > VideoFramePacket.SessionIdMaxLength)
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.SessionId), $"must not exceed {VideoFramePacket.SessionIdMaxLength} character(s) (was {packet.SessionId.Length}).");

            if (!Enum.IsDefined(packet.Encoding))
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.Encoding), $"value {(byte)packet.Encoding} is not a supported encoding.");

            ValidateDimensions(packet.Width, packet.Height);

            if (packet.Payload.Length <= 0 || packet.Payload.Length > VideoFramePacket.MaxPayloadLength)
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.Payload), $"must be between 1 and {VideoFramePacket.MaxPayloadLength} byte(s) (was {packet.Payload.Length}).");
        }

        private static void ValidateDimensions(int width, int height)
        {
            if (width <= 0 || width > VideoFramePacket.MaxDimension)
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.Width), $"must be between 1 and {VideoFramePacket.MaxDimension} (was {width}).");

            if (height <= 0 || height > VideoFramePacket.MaxDimension)
                throw new VideoFramePacketValidationException(nameof(VideoFramePacket.Height), $"must be between 1 and {VideoFramePacket.MaxDimension} (was {height}).");
        }
    }
}
