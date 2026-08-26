using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// An <see cref="IAudioFrameSource"/> backed by FFmpeg's native libav* libraries (via
    /// <c>FFmpeg.AutoGen</c>) — the audio-side counterpart to <see cref="FfmpegVideoFrameSource"/>,
    /// sharing its own native-library-deployment remarks verbatim (not bundled; must be present on the
    /// host separately). Decodes the first audio stream of the source, resamples every frame to
    /// <see cref="FfmpegAudioFrameSourceOptions.TargetSampleRate"/>/<see cref="FfmpegAudioFrameSourceOptions.MaxChannels"/>
    /// (never upmixing beyond the source's own channel count) via <c>libswresample</c>, and yields it as
    /// <see cref="AudioSampleFormat.Float32Interleaved"/> — ready for <see cref="AudioFrameEncoder"/> to
    /// compress into a wire <see cref="AudioFramePacket"/>.
    /// </summary>
    public sealed unsafe class FfmpegAudioFrameSource : IAudioFrameSource
    {
        private static readonly object InitializationLock = new();
        private static bool _initialized;

        private readonly FfmpegAudioFrameSourceOptions _options;
        private readonly AVIOInterruptCB_callback _interruptCallback;

        private AVFormatContext* _formatContext;
        private AVCodecContext* _codecContext;
        private SwrContext* _swrContext;
        private AVPacket* _packet;
        private AVFrame* _frame;
        private int _audioStreamIndex = -1;
        private int _outputChannels;
        private int _outputSampleRate;
        private int _generation;
        private CancellationToken _activeCancellationToken;
        private bool _disposed;

        public FfmpegAudioFrameSource(FfmpegAudioFrameSourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.TargetSampleRate, 0);
            if (options.MaxChannels is not (1 or 2))
                throw new ArgumentOutOfRangeException(nameof(options), options.MaxChannels, $"{nameof(FfmpegAudioFrameSourceOptions.MaxChannels)} must be 1 (mono) or 2 (stereo).");

            _options = options;
            _interruptCallback = OnInterruptRequested;

            EnsureNativeBindingsInitialized(options.RootPath);
        }

        public AudioStreamInfo? StreamInfo { get; private set; }

        private bool IsOpen => _formatContext is not null && _codecContext is not null;

        public Task<AudioStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            ObjectDisposedException.ThrowIf(_disposed, this);

            return Task.Run(() => Open(source, cancellationToken), cancellationToken);
        }

        // See FfmpegVideoFrameSource.ReadFramesAsync's own remarks on why every bit of native FFmpeg
        // interaction happens inside ordinary (non-async) helper methods instead of here directly.
        public async IAsyncEnumerable<DecodedAudioFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!IsOpen)
                throw new InvalidOperationException($"{nameof(ReadFramesAsync)} was called before {nameof(OpenAsync)} completed.");

            cancellationToken.ThrowIfCancellationRequested();

            var myGeneration = Interlocked.Increment(ref _generation);

            if (startPosition > TimeSpan.Zero)
                Seek(startPosition, cancellationToken);

            while (Volatile.Read(ref _generation) == myGeneration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (DecodeNextFrame(cancellationToken) is not { } decodedFrame)
                    yield break;

                if (Volatile.Read(ref _generation) != myGeneration)
                    yield break;

                await Task.Yield();
                yield return decodedFrame;
            }
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
                return ValueTask.CompletedTask;

            _disposed = true;
            Interlocked.Increment(ref _generation);

            if (_swrContext is not null)
            {
                fixed (SwrContext** swrContext = &_swrContext)
                    ffmpeg.swr_free(swrContext);
            }

            if (_frame is not null)
            {
                fixed (AVFrame** frame = &_frame)
                    ffmpeg.av_frame_free(frame);
            }

            if (_packet is not null)
            {
                fixed (AVPacket** packet = &_packet)
                    ffmpeg.av_packet_free(packet);
            }

            if (_codecContext is not null)
            {
                fixed (AVCodecContext** codecContext = &_codecContext)
                    ffmpeg.avcodec_free_context(codecContext);
            }

            if (_formatContext is not null)
            {
                fixed (AVFormatContext** formatContext = &_formatContext)
                    ffmpeg.avformat_close_input(formatContext);
            }

            return ValueTask.CompletedTask;
        }

        private AudioStreamInfo Open(VideoSource source, CancellationToken cancellationToken)
        {
            _activeCancellationToken = cancellationToken;

            var formatContext = ffmpeg.avformat_alloc_context();
            formatContext->interrupt_callback = new AVIOInterruptCB
            {
                callback = _interruptCallback,
                opaque = null
            };

            var openResult = ffmpeg.avformat_open_input(&formatContext, source.Location, null, null);
            if (openResult < 0)
            {
                ThrowIfCancelled(cancellationToken);
                throw new VideoFrameSourceException($"Opening the audio source failed: {GetErrorDescription(openResult)}.");
            }

            _formatContext = formatContext;

            var streamInfoResult = ffmpeg.avformat_find_stream_info(_formatContext, null);
            if (streamInfoResult < 0)
            {
                ThrowIfCancelled(cancellationToken);
                throw new VideoFrameSourceException($"Reading stream information failed: {GetErrorDescription(streamInfoResult)}.");
            }

            AVStream* audioStream = null;
            for (var i = 0u; i < _formatContext->nb_streams; i++)
            {
                var candidate = _formatContext->streams[i];
                if (candidate->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    audioStream = candidate;
                    break;
                }
            }

            if (audioStream is null)
                throw new VideoFrameSourceException("The source has no audio stream.");

            _audioStreamIndex = audioStream->index;

            var decoder = ffmpeg.avcodec_find_decoder(audioStream->codecpar->codec_id);
            if (decoder is null)
                throw new VideoFrameSourceException("No decoder is available for the source's audio codec.");

            _codecContext = ffmpeg.avcodec_alloc_context3(decoder);
            ThrowIfError(ffmpeg.avcodec_parameters_to_context(_codecContext, audioStream->codecpar), "Copying codec parameters");
            ThrowIfError(ffmpeg.avcodec_open2(_codecContext, decoder, null), "Opening the decoder");

            _outputChannels = Math.Min(_codecContext->ch_layout.nb_channels, _options.MaxChannels);
            if (_outputChannels <= 0)
                _outputChannels = _options.MaxChannels;
            _outputSampleRate = _options.TargetSampleRate;

            AVChannelLayout inputLayout = _codecContext->ch_layout;
            AVChannelLayout outputLayout;
            ffmpeg.av_channel_layout_default(&outputLayout, _outputChannels);

            SwrContext* swrContext = null;
            ThrowIfError(
                ffmpeg.swr_alloc_set_opts2(&swrContext, &outputLayout, AVSampleFormat.AV_SAMPLE_FMT_FLT, _outputSampleRate, &inputLayout, _codecContext->sample_fmt, _codecContext->sample_rate, 0, null),
                "Configuring the resampler");
            _swrContext = swrContext;

            ThrowIfError(ffmpeg.swr_init(_swrContext), "Initializing the resampler");

            _packet = ffmpeg.av_packet_alloc();
            _frame = ffmpeg.av_frame_alloc();

            var durationTicks = _formatContext->duration > 0
                ? _formatContext->duration * TimeSpan.TicksPerSecond / ffmpeg.AV_TIME_BASE
                : 0;

            StreamInfo = new AudioStreamInfo
            {
                SampleRate = _outputSampleRate,
                Channels = _outputChannels,
                SampleFormat = AudioSampleFormat.Float32Interleaved,
                Duration = TimeSpan.FromTicks(durationTicks),
                SourceCodecName = ffmpeg.avcodec_get_name(audioStream->codecpar->codec_id)
            };

            return StreamInfo;
        }

        private void Seek(TimeSpan position, CancellationToken cancellationToken)
        {
            _activeCancellationToken = cancellationToken;

            var stream = _formatContext->streams[_audioStreamIndex];
            var timestamp = (long)(position.TotalSeconds / ffmpeg.av_q2d(stream->time_base));

            var seekResult = ffmpeg.av_seek_frame(_formatContext, _audioStreamIndex, timestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
            if (seekResult < 0)
            {
                ThrowIfCancelled(cancellationToken);
                throw new VideoFrameSourceException($"Seeking failed: {GetErrorDescription(seekResult)}.");
            }

            ffmpeg.avcodec_flush_buffers(_codecContext);
        }

        // Mirrors FfmpegVideoFrameSource.DecodeNextFrame's own read-packet / send-to-decoder /
        // receive-frame loop exactly, substituting the audio stream index and resampling in place of
        // scaling. Returns null only at genuine end of stream; any failure throws.
        private DecodedAudioFrame? DecodeNextFrame(CancellationToken cancellationToken)
        {
            _activeCancellationToken = cancellationToken;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var receiveResult = ffmpeg.avcodec_receive_frame(_codecContext, _frame);
                if (receiveResult == 0)
                {
                    var decodedFrame = ResampleFrame(_frame);
                    ffmpeg.av_frame_unref(_frame);
                    return decodedFrame;
                }

                if (receiveResult == ffmpeg.AVERROR_EOF)
                    return null;

                if (receiveResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                    ThrowIfError(receiveResult, "Receiving a decoded frame");

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var readResult = ffmpeg.av_read_frame(_formatContext, _packet);
                    if (readResult == ffmpeg.AVERROR_EOF)
                    {
                        ffmpeg.avcodec_send_packet(_codecContext, null);
                        break;
                    }

                    ThrowIfError(readResult, "Reading a packet");

                    if (_packet->stream_index != _audioStreamIndex)
                    {
                        ffmpeg.av_packet_unref(_packet);
                        continue;
                    }

                    ThrowIfError(ffmpeg.avcodec_send_packet(_codecContext, _packet), "Sending a packet to the decoder");
                    ffmpeg.av_packet_unref(_packet);
                    break;
                }
            }
        }

        private DecodedAudioFrame ResampleFrame(AVFrame* frame)
        {
            var sourceData = new byte*[8];
            for (var i = 0u; i < 8; i++)
                sourceData[i] = frame->data[i];

            // Resampling (rate conversion in particular) can produce more output samples than the input
            // frame's own sample count — size the destination for the worst case this call could ever
            // produce, per libswresample's own documented sizing contract.
            var maxOutputSamples = ffmpeg.swr_get_out_samples(_swrContext, frame->nb_samples);
            var bytesPerSample = sizeof(float) * _outputChannels;
            var buffer = new byte[maxOutputSamples * bytesPerSample];
            var destinationData = new byte*[8];

            int producedSamples;
            fixed (byte* destination = buffer)
            fixed (byte** sourcePtrs = sourceData)
            fixed (byte** destinationPtrs = destinationData)
            {
                destinationPtrs[0] = destination;
                producedSamples = ffmpeg.swr_convert(_swrContext, destinationPtrs, maxOutputSamples, sourcePtrs, frame->nb_samples);
            }

            if (producedSamples < 0)
                ThrowIfError(producedSamples, "Resampling a decoded audio frame");

            var stream = _formatContext->streams[_audioStreamIndex];
            var timeBase = stream->time_base;

            var presentationTimestamp = TimeSpan.FromSeconds(frame->best_effort_timestamp * ffmpeg.av_q2d(timeBase));
            var duration = TimeSpan.FromSeconds(producedSamples / (double)_outputSampleRate);
            var producedBytes = producedSamples * bytesPerSample;

            return new DecodedAudioFrame(presentationTimestamp, duration, _outputSampleRate, _outputChannels, AudioSampleFormat.Float32Interleaved, buffer.AsMemory(0, producedBytes));
        }

        private int OnInterruptRequested(void* opaque) => _activeCancellationToken.IsCancellationRequested ? 1 : 0;

        private void ThrowIfCancelled(CancellationToken cancellationToken) => cancellationToken.ThrowIfCancellationRequested();

        private static void ThrowIfError(int result, string operation)
        {
            if (result < 0)
                throw new VideoFrameSourceException($"{operation} failed: {GetErrorDescription(result)}.");
        }

        private static string GetErrorDescription(int errorCode)
        {
            const int bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(errorCode, buffer, bufferSize);
            return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"error code {errorCode}";
        }

        private static void EnsureNativeBindingsInitialized(string? rootPath)
        {
            if (Volatile.Read(ref _initialized))
                return;

            lock (InitializationLock)
            {
                if (_initialized)
                    return;

                if (!string.IsNullOrWhiteSpace(rootPath))
                    ffmpeg.RootPath = rootPath;

                DynamicallyLoadedBindings.Initialize();
                _initialized = true;
            }
        }
    }
}
