using FFmpeg.AutoGen;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// Compresses a stream of already-decoded <see cref="DecodedAudioFrame"/>s into
    /// <see cref="Encoding"/>-encoded packets, ready to carry as <see cref="AudioFramePacket.Payload"/>
    /// — the audio-side counterpart to <see cref="VideoFrameEncoder"/>, via FFmpeg's own native Opus/AAC
    /// encoders (<c>FFmpeg.AutoGen</c>).
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="VideoFrameEncoder"/>'s stateless, one-call-per-frame design, these encoders are
    /// inherently <i>stateful</i> — each accepts only fixed-size chunks of input samples (its own
    /// <c>frame_size</c>) and may buffer internally before emitting a packet, so this type is an instance
    /// (own encoder state, own accumulated-but-not-yet-encoded samples) rather than a static method: one
    /// instance per session generation, created fresh for each and discarded when that generation ends —
    /// mirroring how a generation's <see cref="IVideoFrameSource"/>/<see cref="IAudioFrameSource"/> are
    /// themselves fresh per generation, never reused across a seek/select. Every input frame must be
    /// <see cref="AudioSampleFormat.Float32Interleaved"/> at this instance's own configured sample
    /// rate/channel count — exactly what <see cref="FfmpegAudioFrameSource"/> always yields; this type
    /// itself handles converting to whichever native sample layout <see cref="Encoding"/>'s own encoder
    /// actually requires (FFmpeg's native AAC encoder requires planar samples, unlike Opus's interleaved
    /// ones — the difference is confined entirely to this type, never leaking into the decode side).
    /// <para/>
    /// Construction itself never touches native FFmpeg — mirroring <see cref="FfmpegVideoFrameSource"/>/
    /// <see cref="FfmpegAudioFrameSource"/>'s own lazy pattern, the actual encoder is opened on first use
    /// (<see cref="Encode"/> or <see cref="Flush"/>), not in the constructor.
    /// </remarks>
    public sealed unsafe class AudioFrameEncoder : IAudioEncoder
    {
        // Used only when the encoder itself does not require a fixed input size — an arbitrary, modest
        // chunk size (20ms at a typical streaming rate) keeps per-packet latency low without emitting an
        // encoded packet for every single decoder-native chunk, whatever size those happen to be.
        private const int DefaultVariableFrameSize = 960;

        private readonly int _sampleRate;
        private readonly int _channels;
        private readonly int _bitRate;

        private AVCodecContext* _codecContext;
        private AVFrame* _frame;
        private AVPacket* _packet;
        private float[]? _accumulator;
        private bool _isPlanar;
        private int _accumulatedSamples;
        private long _nextFramePts;
        private bool _opened;
        private bool _disposed;

        /// <param name="sampleRate">Must match every <see cref="DecodedAudioFrame.SampleRate"/> this instance will ever be given. For <see cref="AudioFramePacketEncoding.Opus"/>, must be one of its own supported rates (8000/12000/16000/24000/48000).</param>
        /// <param name="channels">Must match every <see cref="DecodedAudioFrame.Channels"/> this instance will ever be given. 1 (mono) or 2 (stereo).</param>
        /// <param name="encoding">Which codec to encode to. Default: <see cref="AudioFramePacketEncoding.Opus"/>.</param>
        /// <param name="bitRate">Target bitrate, in bits per second. Default: 64000 (a reasonable voice/music quality/bandwidth balance for streaming, suitable for either codec).</param>
        public AudioFrameEncoder(int sampleRate, int channels, AudioFramePacketEncoding encoding = AudioFramePacketEncoding.Opus, int bitRate = 64_000)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);
            if (channels is not (1 or 2))
                throw new ArgumentOutOfRangeException(nameof(channels), channels, "must be 1 (mono) or 2 (stereo).");
            if (!Enum.IsDefined(encoding))
                throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "is not a supported encoding.");
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bitRate, 0);

            _sampleRate = sampleRate;
            _channels = channels;
            Encoding = encoding;
            _bitRate = bitRate;
        }

        /// <summary>Which codec this instance encodes to — the same value every <see cref="EncodedAudioChunk"/> it produces is implicitly encoded as (a caller stamps this onto <see cref="AudioFramePacket.Encoding"/> itself; this type has no opinion on packet construction).</summary>
        public AudioFramePacketEncoding Encoding { get; }

        /// <summary>Samples per channel this instance sends the encoder in every call — <see cref="DecodedAudioFrame"/>s are accumulated until this many are available. Zero until the first <see cref="Encode"/>/<see cref="Flush"/> call has opened the encoder.</summary>
        public int FrameSize { get; private set; }

        /// <summary>
        /// Accumulates <paramref name="frame"/>'s own samples and encodes as many complete
        /// <see cref="FrameSize"/>-sample chunks as are now available — zero, one, or more, since a
        /// single input frame's sample count rarely lines up exactly with <see cref="FrameSize"/> and the
        /// encoder itself may buffer internally before actually emitting a packet.
        /// </summary>
        /// <exception cref="NotSupportedException"><paramref name="frame"/>'s own format/rate/channel count does not match this instance's own configuration.</exception>
        /// <exception cref="VideoFrameSourceException">No encoder for <see cref="Encoding"/> is available, or it could not be configured/opened.</exception>
        public IReadOnlyList<EncodedAudioChunk> Encode(DecodedAudioFrame frame)
        {
            ArgumentNullException.ThrowIfNull(frame);
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureOpened();

            if (frame.SampleFormat != AudioSampleFormat.Float32Interleaved || frame.SampleRate != _sampleRate || frame.Channels != _channels)
                throw new NotSupportedException($"This encoder only accepts {AudioSampleFormat.Float32Interleaved} at {_sampleRate} Hz / {_channels} channel(s) — got {frame.SampleFormat} at {frame.SampleRate} Hz / {frame.Channels} channel(s).");

            var incoming = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(frame.Data.Span);
            var chunks = new List<EncodedAudioChunk>();
            var offset = 0;

            while (offset < incoming.Length)
            {
                var capacity = _accumulator!.Length - _accumulatedSamples * _channels;
                var toCopy = Math.Min(capacity, incoming.Length - offset);

                incoming.Slice(offset, toCopy).CopyTo(_accumulator.AsSpan(_accumulatedSamples * _channels, toCopy));
                _accumulatedSamples += toCopy / _channels;
                offset += toCopy;

                if (_accumulatedSamples >= FrameSize)
                    EncodeAccumulatedFrame(chunks);
            }

            return chunks;
        }

        /// <summary>Signals end of stream to the encoder and drains every packet it was still internally buffering. Call once, when no further <see cref="Encode"/> call will ever follow.</summary>
        public IReadOnlyList<EncodedAudioChunk> Flush()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureOpened();

            var chunks = new List<EncodedAudioChunk>();
            ffmpeg.avcodec_send_frame(_codecContext, null);
            DrainPackets(chunks);
            return chunks;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_frame is not null)
            {
                var frame = _frame;
                ffmpeg.av_frame_free(&frame);
            }

            if (_packet is not null)
            {
                var packet = _packet;
                ffmpeg.av_packet_free(&packet);
            }

            if (_codecContext is not null)
            {
                var codecContext = _codecContext;
                ffmpeg.avcodec_free_context(&codecContext);
            }
        }

        private void EnsureOpened()
        {
            if (_opened)
                return;

            var codecId = Encoding switch
            {
                AudioFramePacketEncoding.Opus => AVCodecID.AV_CODEC_ID_OPUS,
                AudioFramePacketEncoding.Aac => AVCodecID.AV_CODEC_ID_AAC,
                _ => throw new NotSupportedException($"{Encoding} has no known native encoder mapping.")
            };

            // FFmpeg's own native encoders: "opus" takes interleaved float samples; "aac" — unlike
            // Opus — only accepts planar float samples (one contiguous buffer per channel, not
            // interleaved). This is the one place that difference is handled; see this type's own
            // remarks on why it never leaks into DecodedAudioFrame/FfmpegAudioFrameSource.
            var nativeSampleFormat = Encoding == AudioFramePacketEncoding.Aac ? AVSampleFormat.AV_SAMPLE_FMT_FLTP : AVSampleFormat.AV_SAMPLE_FMT_FLT;
            _isPlanar = nativeSampleFormat == AVSampleFormat.AV_SAMPLE_FMT_FLTP;

            var encoder = ffmpeg.avcodec_find_encoder(codecId);
            if (encoder is null)
                throw new VideoFrameSourceException($"No {Encoding} encoder is available.");

            _codecContext = ffmpeg.avcodec_alloc_context3(encoder);
            _codecContext->sample_rate = _sampleRate;
            _codecContext->sample_fmt = nativeSampleFormat;
            _codecContext->bit_rate = _bitRate;
            _codecContext->time_base = new AVRational { num = 1, den = _sampleRate };
            ffmpeg.av_channel_layout_default(&_codecContext->ch_layout, _channels);

            var openResult = ffmpeg.avcodec_open2(_codecContext, encoder, null);
            if (openResult < 0)
            {
                var codecContext = _codecContext;
                ffmpeg.avcodec_free_context(&codecContext);
                _codecContext = null;
                throw new VideoFrameSourceException($"Opening the {Encoding} encoder failed: error code {openResult}.");
            }

            // A frame_size of 0 means this encoder accepts any input size (AV_CODEC_CAP_VARIABLE_FRAME_SIZE)
            // — the accumulator below then just passes every frame straight through, one chunk at a time.
            // AAC's own native encoder always reports a fixed 1024 here; Opus's depends on how it was
            // configured — this reads whatever the actually-opened encoder reports either way, rather
            // than hardcoding either codec's own typical value.
            FrameSize = _codecContext->frame_size > 0 ? _codecContext->frame_size : DefaultVariableFrameSize;

            _frame = ffmpeg.av_frame_alloc();
            _frame->format = (int)nativeSampleFormat;
            _frame->sample_rate = _sampleRate;
            ffmpeg.av_channel_layout_default(&_frame->ch_layout, _channels);
            _frame->nb_samples = FrameSize;
            ThrowIfError(ffmpeg.av_frame_get_buffer(_frame, 0), "Allocating the encoder's own input frame buffer");

            _packet = ffmpeg.av_packet_alloc();
            _accumulator = new float[FrameSize * _channels];
            _opened = true;
        }

        private void EncodeAccumulatedFrame(List<EncodedAudioChunk> chunks)
        {
            ThrowIfError(ffmpeg.av_frame_make_writable(_frame), "Preparing the encoder's own input frame");

            var source = _accumulator!.AsSpan(0, FrameSize * _channels);

            if (_isPlanar)
            {
                // De-interleave: _accumulator is channel-interleaved (LRLRLR...), but a planar frame's
                // data[c] must hold channel c's own samples contiguously.
                for (var channel = 0; channel < _channels; channel++)
                {
                    var destination = new Span<float>(_frame->data[(uint)channel], FrameSize);
                    for (var sample = 0; sample < FrameSize; sample++)
                        destination[sample] = source[sample * _channels + channel];
                }
            }
            else
            {
                var destination = new Span<float>(_frame->data[0], FrameSize * _channels);
                source.CopyTo(destination);
            }

            _frame->pts = _nextFramePts;
            _nextFramePts += FrameSize;

            var sendResult = ffmpeg.avcodec_send_frame(_codecContext, _frame);
            if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                ThrowIfError(sendResult, $"Sending a frame to the {Encoding} encoder");

            DrainPackets(chunks);

            // Shift any leftover samples (beyond this one FrameSize-sized chunk) down to the front.
            var remaining = _accumulatedSamples - FrameSize;
            if (remaining > 0)
                Array.Copy(_accumulator!, FrameSize * _channels, _accumulator!, 0, remaining * _channels);
            _accumulatedSamples = remaining;
        }

        private void DrainPackets(List<EncodedAudioChunk> chunks)
        {
            while (true)
            {
                var receiveResult = ffmpeg.avcodec_receive_packet(_codecContext, _packet);
                if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
                    return;

                ThrowIfError(receiveResult, $"Receiving an encoded {Encoding} packet");

                var payload = new byte[_packet->size];
                new Span<byte>(_packet->data, _packet->size).CopyTo(payload);

                var presentationTimestamp = TimeSpan.FromSeconds(_packet->pts * ffmpeg.av_q2d(_codecContext->time_base));
                var duration = TimeSpan.FromSeconds(FrameSize / (double)_sampleRate);

                chunks.Add(new EncodedAudioChunk(payload, presentationTimestamp, duration));

                ffmpeg.av_packet_unref(_packet);
            }
        }

        private static void ThrowIfError(int result, string operation)
        {
            if (result < 0)
                throw new VideoFrameSourceException($"{operation} failed: error code {result}.");
        }
    }
}
