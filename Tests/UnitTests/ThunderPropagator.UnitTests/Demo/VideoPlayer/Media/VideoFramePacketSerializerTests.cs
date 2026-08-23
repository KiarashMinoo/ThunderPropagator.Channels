using System.Buffers.Binary;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Issue #214's own ACs: every required field round-trips losslessly through the binary wire
    /// format, malformed lengths/unsupported encodings/oversized payloads are rejected rather than
    /// misparsed, and the write path does not allocate beyond what the caller's destination buffer
    /// already accounts for.
    /// </summary>
    public sealed class VideoFramePacketSerializerTests
    {
        private static VideoFramePacket CreatePacket(int payloadLength = 16, VideoFramePacketEncoding encoding = VideoFramePacketEncoding.Jpeg, string sessionId = "session-1")
        {
            var payload = new byte[payloadLength];
            for (var i = 0; i < payload.Length; i++)
                payload[i] = (byte)(i % 256);

            return new VideoFramePacket
            {
                SessionId = sessionId,
                Epoch = 3,
                FrameNumber = 123_456,
                PresentationTimestamp = TimeSpan.FromSeconds(12.5),
                Duration = TimeSpan.FromMilliseconds(33.37),
                DisplayTime = TimeSpan.FromSeconds(12.6),
                Width = 1920,
                Height = 1080,
                Encoding = encoding,
                Payload = payload
            };
        }

        [Theory]
        [InlineData(VideoFramePacketEncoding.Jpeg)]
        [InlineData(VideoFramePacketEncoding.WebP)]
        public void WriteThenRead_RoundTripsEveryFieldLosslessly(VideoFramePacketEncoding encoding)
        {
            var original = CreatePacket(payloadLength: 4096, encoding: encoding, sessionId: "session-ألف-🎬");

            var buffer = VideoFramePacketSerializer.Write(original);
            var roundTripped = VideoFramePacketSerializer.Read(buffer);

            Assert.Equal(original, roundTripped);
            Assert.True(original.Payload.Span.SequenceEqual(roundTripped.Payload.Span));
        }

        [Fact]
        public void Write_ReportsExactSizeMatchingGetSize()
        {
            var packet = CreatePacket(payloadLength: 777);

            var expectedSize = VideoFramePacketSerializer.GetSize(packet);
            var buffer = new byte[expectedSize];
            var bytesWritten = VideoFramePacketSerializer.Write(packet, buffer);

            Assert.Equal(expectedSize, bytesWritten);
        }

        [Fact]
        public void Write_ToDestinationSmallerThanGetSize_Throws()
        {
            var packet = CreatePacket();
            var tooSmall = new byte[VideoFramePacketSerializer.GetSize(packet) - 1];

            Assert.Throws<ArgumentException>(() => VideoFramePacketSerializer.Write(packet, tooSmall));
        }

        [Fact]
        public void Read_TwoPacketsBackToBack_ReadEachInTurnViaBytesConsumed()
        {
            var first = CreatePacket(payloadLength: 10, sessionId: "first");
            var second = CreatePacket(payloadLength: 20, sessionId: "second");

            var buffer = new byte[VideoFramePacketSerializer.GetSize(first) + VideoFramePacketSerializer.GetSize(second)];
            var firstSize = VideoFramePacketSerializer.Write(first, buffer);
            VideoFramePacketSerializer.Write(second, buffer.AsSpan(firstSize));

            var readFirst = VideoFramePacketSerializer.Read(buffer, out var consumed);
            var readSecond = VideoFramePacketSerializer.Read(buffer.AsSpan(consumed), out _);

            Assert.Equal(first, readFirst);
            Assert.Equal(second, readSecond);
        }

        [Fact]
        public void Read_WithTrailingBytes_Throws()
        {
            var packet = CreatePacket();
            var buffer = VideoFramePacketSerializer.Write(packet);
            var withTrailingGarbage = buffer.Concat(new byte[] { 1, 2, 3 }).ToArray();

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Read(withTrailingGarbage));
        }

        [Fact]
        public void Read_TruncatedBeforeFixedHeaderCompletes_Throws()
        {
            var buffer = VideoFramePacketSerializer.Write(CreatePacket());

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Read(buffer.AsSpan(0, 10)));
        }

        [Fact]
        public void Read_TruncatedMidPayload_Throws()
        {
            var buffer = VideoFramePacketSerializer.Write(CreatePacket(payloadLength: 100));

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Read(buffer.AsSpan(0, buffer.Length - 10)));
        }

        [Fact]
        public void Read_WithUnsupportedFormatVersion_Throws()
        {
            var buffer = VideoFramePacketSerializer.Write(CreatePacket());
            buffer[0] = VideoFramePacketSerializer.CurrentFormatVersion + 1;

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Read(buffer));
        }

        [Fact]
        public void Read_WithUndefinedEncodingByte_Throws()
        {
            var buffer = VideoFramePacketSerializer.Write(CreatePacket());
            buffer[1] = byte.MaxValue;

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Read(buffer));
        }

        [Fact]
        public void Read_WithNegativeSessionIdLength_Throws()
        {
            var buffer = VideoFramePacketSerializer.Write(CreatePacket());
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(46), -1);

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Read(buffer));
        }

        [Fact]
        public void Read_WithSessionIdLengthExceedingBuffer_Throws()
        {
            var buffer = VideoFramePacketSerializer.Write(CreatePacket());
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(46), int.MaxValue / 2);

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Read(buffer));
        }

        [Fact]
        public void Read_WithPayloadLengthExceedingMaximum_Throws()
        {
            var packet = CreatePacket();
            var buffer = VideoFramePacketSerializer.Write(packet);
            var payloadLengthOffset = buffer.Length - packet.Payload.Length - sizeof(int);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(payloadLengthOffset), VideoFramePacket.MaxPayloadLength + 1);

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Read(buffer));
        }

        [Theory]
        [InlineData(0, 1080)]
        [InlineData(1920, 0)]
        [InlineData(-1, 1080)]
        [InlineData(1920, VideoFramePacket.MaxDimension + 1)]
        public void Write_WithInvalidDimensions_Throws(int width, int height)
        {
            var packet = CreatePacket() with { Width = width, Height = height };

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Write(packet));
        }

        [Fact]
        public void Write_WithEmptyPayload_Throws()
        {
            var packet = CreatePacket() with { Payload = ReadOnlyMemory<byte>.Empty };

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Write(packet));
        }

        [Fact]
        public void Write_WithPayloadExceedingMaximum_Throws()
        {
            var packet = CreatePacket() with { Payload = new byte[VideoFramePacket.MaxPayloadLength + 1] };

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Write(packet));
        }

        [Fact]
        public void Write_WithSessionIdTooLong_Throws()
        {
            var packet = CreatePacket() with { SessionId = new string('s', VideoFramePacket.SessionIdMaxLength + 1) };

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Write(packet));
        }

        [Fact]
        public void Write_WithUndefinedEncoding_Throws()
        {
            var packet = CreatePacket() with { Encoding = (VideoFramePacketEncoding)byte.MaxValue };

            Assert.Throws<VideoFramePacketValidationException>(() => VideoFramePacketSerializer.Write(packet));
        }

        // Issue #214's own AC: packet serialization is covered by allocation-focused tests. Writing
        // into an already-sized, reused destination span should never allocate — the payload bytes are
        // copied directly via Span.CopyTo, and every fixed field is written with BinaryPrimitives, so
        // nothing here should box, allocate a temporary array, or grow a buffer.
        [Fact]
        public void Write_ToPreSizedSpan_AllocatesNothing()
        {
            var packet = CreatePacket(payloadLength: 8192);
            var buffer = new byte[VideoFramePacketSerializer.GetSize(packet)];

            VideoFramePacketSerializer.Write(packet, buffer); // warm up JIT before measuring

            var before = GC.GetAllocatedBytesForCurrentThread();
            VideoFramePacketSerializer.Write(packet, buffer);
            var after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(before, after);
        }
    }
}
