using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;
using ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// Issue #224's own AC: "Audio bytes are published as binary packets, never Base64-in-JSON" — this
    /// binary round-trip is what makes that possible. Mirrors <see cref="VideoFramePacketSerializerTests"/>'s
    /// own coverage shape for the audio side.
    /// </summary>
    public sealed class AudioFramePacketSerializerTests
    {
        private static AudioFramePacket CreatePacket(string sessionId = "session-1", int epoch = 3, long packetNumber = 7) => new()
        {
            SessionId = sessionId,
            Epoch = epoch,
            PacketNumber = packetNumber,
            PresentationTimestamp = TimeSpan.FromMilliseconds(140),
            Duration = TimeSpan.FromMilliseconds(20),
            DisplayTime = TimeSpan.FromMilliseconds(141),
            SampleRate = 48_000,
            Channels = 2,
            Encoding = AudioFramePacketEncoding.Opus,
            Payload = new byte[] { 1, 2, 3, 4, 5 }
        };

        [Fact]
        public void WriteThenRead_RoundTripsAllFieldsLosslessly()
        {
            var packet = CreatePacket();

            var bytes = AudioFramePacketSerializer.Write(packet);
            var roundTripped = AudioFramePacketSerializer.Read(bytes);

            Assert.Equal(packet, roundTripped);
        }

        [Fact]
        public void GetSize_MatchesTheActualNumberOfBytesWritten()
        {
            var packet = CreatePacket();

            var expectedSize = AudioFramePacketSerializer.GetSize(packet);
            var bytes = AudioFramePacketSerializer.Write(packet);

            Assert.Equal(expectedSize, bytes.Length);
        }

        [Fact]
        public void Read_WithTrailingBytes_Throws()
        {
            var bytes = AudioFramePacketSerializer.Write(CreatePacket());
            var withTrailingByte = bytes.Concat([(byte)0]).ToArray();

            Assert.Throws<AudioFramePacketValidationException>(() => AudioFramePacketSerializer.Read(withTrailingByte));
        }

        [Fact]
        public void Read_WithBytesConsumed_AllowsFramingMultiplePacketsBackToBack()
        {
            var first = AudioFramePacketSerializer.Write(CreatePacket(packetNumber: 1));
            var second = AudioFramePacketSerializer.Write(CreatePacket(packetNumber: 2));
            var combined = first.Concat(second).ToArray();

            var decodedFirst = AudioFramePacketSerializer.Read(combined, out var consumed);
            var decodedSecond = AudioFramePacketSerializer.Read(combined.AsSpan(consumed), out var consumedSecond);

            Assert.Equal(1, decodedFirst.PacketNumber);
            Assert.Equal(2, decodedSecond.PacketNumber);
            Assert.Equal(combined.Length, consumed + consumedSecond);
        }

        [Fact]
        public void Read_TooShortForFixedHeader_Throws()
        {
            Assert.Throws<AudioFramePacketValidationException>(() => AudioFramePacketSerializer.Read(new byte[10]));
        }

        [Fact]
        public void Read_WithUnsupportedFormatVersion_Throws()
        {
            var bytes = AudioFramePacketSerializer.Write(CreatePacket());
            bytes[0] = 255;

            Assert.Throws<AudioFramePacketValidationException>(() => AudioFramePacketSerializer.Read(bytes));
        }

        [Fact]
        public void Read_WithUnsupportedEncoding_Throws()
        {
            var bytes = AudioFramePacketSerializer.Write(CreatePacket());
            bytes[1] = 255;

            Assert.Throws<AudioFramePacketValidationException>(() => AudioFramePacketSerializer.Read(bytes));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(-1)]
        public void Write_WithInvalidChannels_Throws(int channels)
        {
            var packet = CreatePacket() with { Channels = channels };

            Assert.Throws<AudioFramePacketValidationException>(() => AudioFramePacketSerializer.Write(packet));
        }

        [Fact]
        public void Write_WithEmptySessionId_Throws()
        {
            var packet = CreatePacket(sessionId: "");

            Assert.Throws<AudioFramePacketValidationException>(() => AudioFramePacketSerializer.Write(packet));
        }

        [Fact]
        public void Write_WithEmptyPayload_Throws()
        {
            var packet = CreatePacket() with { Payload = ReadOnlyMemory<byte>.Empty };

            Assert.Throws<AudioFramePacketValidationException>(() => AudioFramePacketSerializer.Write(packet));
        }

        [Fact]
        public void Write_WithPayloadExceedingMaxLength_Throws()
        {
            var packet = CreatePacket() with { Payload = new byte[AudioFramePacket.MaxPayloadLength + 1] };

            Assert.Throws<AudioFramePacketValidationException>(() => AudioFramePacketSerializer.Write(packet));
        }

        [Fact]
        public void Equals_ComparesPayloadContent_NotBufferIdentity()
        {
            var first = CreatePacket() with { Payload = new byte[] { 9, 9, 9 } };
            var second = CreatePacket() with { Payload = new byte[] { 9, 9, 9 } };

            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }
    }
}
