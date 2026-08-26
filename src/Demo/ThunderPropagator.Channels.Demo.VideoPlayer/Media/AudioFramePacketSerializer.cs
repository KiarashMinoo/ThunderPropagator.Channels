using System.Buffers.Binary;
using System.Text;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Encodes/decodes an <see cref="AudioFramePacket"/> to/from a compact, length-prefixed binary
    /// layout — the audio-side counterpart to <see cref="VideoFramePacketSerializer"/>, sharing its own
    /// layout shape (fixed header, then <see cref="AudioFramePacket.SessionId"/>, then
    /// <see cref="AudioFramePacket.Payload"/>) with <c>SampleRate</c>/<c>Channels</c> in place of
    /// <c>Width</c>/<c>Height</c>.
    /// </summary>
    /// <remarks>
    /// Wire layout (all multi-byte integers little-endian):
    /// <code>
    /// [0]        FormatVersion   (byte)
    /// [1]        Encoding        (byte)
    /// [2..6)     Epoch           (int32)
    /// [6..10)    SampleRate      (int32)
    /// [10..14)   Channels        (int32)
    /// [14..22)   PacketNumber    (int64)
    /// [22..30)   PresentationTimestamp.Ticks (int64)
    /// [30..38)   Duration.Ticks  (int64)
    /// [38..46)   DisplayTime.Ticks (int64)
    /// [46..50)   SessionId byte length (int32)
    /// [50..50+S) SessionId UTF-8 bytes
    /// [50+S..+4) Payload byte length (int32)
    /// [.. +P)    Payload bytes
    /// </code>
    /// <see cref="CurrentFormatVersion"/> is checked on every read, so a future incompatible layout
    /// change can bump it and have old readers reject the new format cleanly instead of misparsing it.
    /// </remarks>
    public static class AudioFramePacketSerializer
    {
        /// <summary>The only wire format version this serializer currently reads or writes.</summary>
        public const byte CurrentFormatVersion = 1;

        private const int FormatVersionOffset = 0;
        private const int EncodingOffset = 1;
        private const int EpochOffset = 2;
        private const int SampleRateOffset = 6;
        private const int ChannelsOffset = 10;
        private const int PacketNumberOffset = 14;
        private const int PresentationTimestampOffset = 22;
        private const int DurationOffset = 30;
        private const int DisplayTimeOffset = 38;
        private const int SessionIdLengthOffset = 46;

        /// <summary>Size, in bytes, of every fixed-position field before the variable-length <see cref="AudioFramePacket.SessionId"/>.</summary>
        private const int FixedHeaderSize = 50;

        /// <summary>The exact number of bytes <see cref="Write(AudioFramePacket,Span{byte})"/> will write for <paramref name="packet"/>.</summary>
        public static int GetSize(AudioFramePacket packet)
        {
            ArgumentNullException.ThrowIfNull(packet);

            return FixedHeaderSize + Encoding.UTF8.GetByteCount(packet.SessionId) + sizeof(int) + packet.Payload.Length;
        }

        /// <summary>Serializes <paramref name="packet"/> into a freshly allocated array sized exactly to fit it.</summary>
        public static byte[] Write(AudioFramePacket packet)
        {
            var buffer = new byte[GetSize(packet)];
            Write(packet, buffer);
            return buffer;
        }

        /// <summary>Serializes <paramref name="packet"/> into <paramref name="destination"/>, which must be at least <see cref="GetSize"/> bytes.</summary>
        /// <returns>The number of bytes actually written — always equal to <see cref="GetSize"/>.</returns>
        public static int Write(AudioFramePacket packet, Span<byte> destination)
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
            BinaryPrimitives.WriteInt32LittleEndian(destination[SampleRateOffset..], packet.SampleRate);
            BinaryPrimitives.WriteInt32LittleEndian(destination[ChannelsOffset..], packet.Channels);
            BinaryPrimitives.WriteInt64LittleEndian(destination[PacketNumberOffset..], packet.PacketNumber);
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
        /// Deserializes exactly one packet from <paramref name="source"/>, which must contain nothing but
        /// that one packet — throws <see cref="AudioFramePacketValidationException"/> if any byte remains
        /// afterward. Use <see cref="Read(ReadOnlySpan{byte},out int)"/> instead when <paramref name="source"/>
        /// may contain more than one packet back-to-back.
        /// </summary>
        public static AudioFramePacket Read(ReadOnlySpan<byte> source)
        {
            var packet = Read(source, out var bytesConsumed);

            if (bytesConsumed != source.Length)
                throw new AudioFramePacketValidationException(nameof(source), $"has {source.Length - bytesConsumed} unexpected trailing byte(s) after one complete packet.");

            return packet;
        }

        /// <summary>
        /// Deserializes one packet from the start of <paramref name="source"/>, reporting how many bytes
        /// it actually consumed via <paramref name="bytesConsumed"/>. The returned packet's
        /// <see cref="AudioFramePacket.Payload"/> is always a private copy of the corresponding bytes in
        /// <paramref name="source"/>.
        /// </summary>
        /// <exception cref="AudioFramePacketValidationException">
        /// <paramref name="source"/> is truncated, declares an unsupported format version or encoding, or
        /// declares a session-id/payload length that is invalid or does not fit in the remaining buffer.
        /// </exception>
        public static AudioFramePacket Read(ReadOnlySpan<byte> source, out int bytesConsumed)
        {
            if (source.Length < FixedHeaderSize)
                throw new AudioFramePacketValidationException(nameof(source), $"must contain at least {FixedHeaderSize} fixed header byte(s) (had {source.Length}).");

            var formatVersion = source[FormatVersionOffset];
            if (formatVersion != CurrentFormatVersion)
                throw new AudioFramePacketValidationException(nameof(formatVersion), $"is {formatVersion}, but only {CurrentFormatVersion} is supported.");

            var encodingValue = source[EncodingOffset];
            var encoding = (AudioFramePacketEncoding)encodingValue;
            if (!Enum.IsDefined(encoding))
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.Encoding), $"value {encodingValue} is not a supported encoding.");

            var epoch = BinaryPrimitives.ReadInt32LittleEndian(source[EpochOffset..]);
            var sampleRate = BinaryPrimitives.ReadInt32LittleEndian(source[SampleRateOffset..]);
            var channels = BinaryPrimitives.ReadInt32LittleEndian(source[ChannelsOffset..]);
            ValidateSampleRateAndChannels(sampleRate, channels);

            var packetNumber = BinaryPrimitives.ReadInt64LittleEndian(source[PacketNumberOffset..]);
            var presentationTimestamp = TimeSpan.FromTicks(BinaryPrimitives.ReadInt64LittleEndian(source[PresentationTimestampOffset..]));
            var duration = TimeSpan.FromTicks(BinaryPrimitives.ReadInt64LittleEndian(source[DurationOffset..]));
            var displayTime = TimeSpan.FromTicks(BinaryPrimitives.ReadInt64LittleEndian(source[DisplayTimeOffset..]));

            var sessionIdByteLength = BinaryPrimitives.ReadInt32LittleEndian(source[SessionIdLengthOffset..]);
            if (sessionIdByteLength <= 0 || sessionIdByteLength > AudioFramePacket.SessionIdMaxLength * 4)
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.SessionId), $"declared byte length ({sessionIdByteLength}) is invalid.");

            var cursor = FixedHeaderSize;
            if (source.Length < cursor + sessionIdByteLength + sizeof(int))
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.SessionId), "declared length extends beyond the available buffer.");

            var sessionId = Encoding.UTF8.GetString(source.Slice(cursor, sessionIdByteLength));
            if (sessionId.Length > AudioFramePacket.SessionIdMaxLength)
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.SessionId), $"must not exceed {AudioFramePacket.SessionIdMaxLength} character(s) (was {sessionId.Length}).");
            cursor += sessionIdByteLength;

            var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(source[cursor..]);
            cursor += sizeof(int);

            if (payloadLength <= 0 || payloadLength > AudioFramePacket.MaxPayloadLength)
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.Payload), $"declared length ({payloadLength}) must be between 1 and {AudioFramePacket.MaxPayloadLength} byte(s).");

            if (source.Length < cursor + payloadLength)
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.Payload), "declared length extends beyond the available buffer.");

            var payload = source.Slice(cursor, payloadLength).ToArray();
            cursor += payloadLength;

            bytesConsumed = cursor;

            return new AudioFramePacket
            {
                SessionId = sessionId,
                Epoch = epoch,
                PacketNumber = packetNumber,
                PresentationTimestamp = presentationTimestamp,
                Duration = duration,
                DisplayTime = displayTime,
                SampleRate = sampleRate,
                Channels = channels,
                Encoding = encoding,
                Payload = payload
            };
        }

        private static void Validate(AudioFramePacket packet)
        {
            if (string.IsNullOrWhiteSpace(packet.SessionId))
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.SessionId), "must not be null, empty, or whitespace-only.");

            if (packet.SessionId.Length > AudioFramePacket.SessionIdMaxLength)
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.SessionId), $"must not exceed {AudioFramePacket.SessionIdMaxLength} character(s) (was {packet.SessionId.Length}).");

            if (!Enum.IsDefined(packet.Encoding))
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.Encoding), $"value {(byte)packet.Encoding} is not a supported encoding.");

            ValidateSampleRateAndChannels(packet.SampleRate, packet.Channels);

            if (packet.Payload.Length <= 0 || packet.Payload.Length > AudioFramePacket.MaxPayloadLength)
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.Payload), $"must be between 1 and {AudioFramePacket.MaxPayloadLength} byte(s) (was {packet.Payload.Length}).");
        }

        private static void ValidateSampleRateAndChannels(int sampleRate, int channels)
        {
            if (sampleRate <= 0)
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.SampleRate), $"must be positive (was {sampleRate}).");

            if (channels is not (1 or 2))
                throw new AudioFramePacketValidationException(nameof(AudioFramePacket.Channels), $"must be 1 (mono) or 2 (stereo) (was {channels}).");
        }
    }
}
