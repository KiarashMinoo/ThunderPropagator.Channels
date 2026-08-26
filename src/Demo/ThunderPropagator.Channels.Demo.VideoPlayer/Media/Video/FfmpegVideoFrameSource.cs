using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video
{
    /// <summary>
    /// An <see cref="IVideoFrameSource"/> backed by FFmpeg's native libav* libraries (via
    /// <c>FFmpeg.AutoGen</c>) — the concrete implementation #216's abstraction exists to make
    /// replaceable. Decodes the first video stream of an approved source in its own presentation
    /// order, scales each frame to <see cref="FfmpegVideoFrameSourceOptions.MaxWidth"/>/
    /// <see cref="FfmpegVideoFrameSourceOptions.MaxHeight"/> (preserving aspect ratio, never upscaling)
    /// via <c>libswscale</c>, and yields it as <see cref="VideoPixelFormat.Bgra32"/> — ready for
    /// <see cref="VideoFrameEncoder"/> to compress into a wire <see cref="VideoFramePacket"/>.
    /// </summary>
    /// <remarks>
    /// <b>Native library deployment:</b> this type P/Invokes into FFmpeg's shared libraries
    /// (<c>avformat</c>, <c>avcodec</c>, <c>avutil</c>, <c>swscale</c>) — they are <i>not</i> bundled
    /// with this package and must be present on the host machine, separately, for every platform this
    /// runs on:
    /// <list type="bullet">
    /// <item><description><b>Windows:</b> the matching-architecture (x64/ARM64) FFmpeg shared build's <c>.dll</c> files (<c>avformat-*.dll</c>, <c>avcodec-*.dll</c>, <c>avutil-*.dll</c>, <c>swscale-*.dll</c>, and their own dependencies), either on <c>PATH</c> or in the directory set via <see cref="FfmpegVideoFrameSourceOptions.RootPath"/>.</description></item>
    /// <item><description><b>Linux:</b> the distribution's <c>ffmpeg</c>/<c>libav*</c> shared libraries (e.g. Debian/Ubuntu's <c>libavformat*.so</c> family), discoverable via the dynamic linker (<c>ldconfig</c>/<c>LD_LIBRARY_PATH</c>) or <see cref="FfmpegVideoFrameSourceOptions.RootPath"/>.</description></item>
    /// <item><description><b>macOS:</b> FFmpeg's <c>.dylib</c> files (e.g. via Homebrew's <c>ffmpeg</c> formula), discoverable via <c>DYLD_LIBRARY_PATH</c> or <see cref="FfmpegVideoFrameSourceOptions.RootPath"/>.</description></item>
    /// </list>
    /// A missing or mismatched-architecture native library surfaces as a <see cref="VideoFrameSourceException"/>
    /// from <see cref="OpenAsync"/>, not a crash — but only once actually invoked, since the libraries
    /// load lazily on first use.
    /// </remarks>
    public sealed unsafe class FfmpegVideoFrameSource : IVideoFrameSource
    {
        // libswscale/swscale.h's SWS_BILINEAR flag — not exposed as a named constant by this version of
        // FFmpeg.AutoGen (its header parser only picked up the algorithm-tuning constants, not the
        // scaler-selection flags themselves), but this integer value is part of libswscale's stable
        // public ABI and has been unchanged since the flag was introduced.
        private const int SwsBilinear = 2;

        private static readonly object InitializationLock = new();
        private static bool _initialized;

        private readonly FfmpegVideoFrameSourceOptions _options;
        private readonly AVIOInterruptCB_callback _interruptCallback;

        private AVFormatContext* _formatContext;
        private AVCodecContext* _codecContext;
        private SwsContext* _swsContext;
        private AVPacket* _packet;
        private AVFrame* _frame;
        private int _videoStreamIndex = -1;
        private byte[]? _scaledFrameBuffer;
        private int _scaledWidth;
        private int _scaledHeight;
        private int _generation;
        private CancellationToken _activeCancellationToken;
        private bool _disposed;

        public FfmpegVideoFrameSource(FfmpegVideoFrameSourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _options = options;
            _interruptCallback = OnInterruptRequested;

            EnsureNativeBindingsInitialized(options.RootPath);
        }

        public VideoStreamInfo? StreamInfo { get; private set; }

        // An ordinary (non-async) property, unlike a field access or pointer comparison, is safe to
        // call directly from ReadFramesAsync's own body — see that method's own remarks on why it
        // cannot touch a pointer itself.
        private bool IsOpen => _formatContext is not null && _codecContext is not null;

        public Task<VideoStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            ObjectDisposedException.ThrowIf(_disposed, this);

            return Task.Run(() => Open(source, cancellationToken), cancellationToken);
        }

        // C# forbids unsafe/pointer constructs anywhere in an async method's own body (a pointer cannot
        // survive the state machine's heap allocation across an await/yield suspension point) — even
        // though this whole class is declared unsafe. So this method itself never touches a pointer or
        // a pointer-typed field directly; every bit of native FFmpeg interaction happens inside ordinary
        // (non-async) helper methods it calls — DecodeNextFrame, Seek, IsOpen.
        public async IAsyncEnumerable<DecodedVideoFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                    yield break; // end of stream

                if (Volatile.Read(ref _generation) != myGeneration)
                    yield break; // superseded by a newer ReadFramesAsync call — see this interface's own contract

                await Task.Yield();
                yield return decodedFrame;
            }
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
                return ValueTask.CompletedTask;

            _disposed = true;
            Interlocked.Increment(ref _generation); // abandon any in-progress ReadFramesAsync enumeration

            if (_swsContext is not null)
            {
                ffmpeg.sws_freeContext(_swsContext);
                _swsContext = null;
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

        private VideoStreamInfo Open(VideoSource source, CancellationToken cancellationToken)
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
                throw new VideoFrameSourceException($"Opening the video source failed: {GetErrorDescription(openResult)}.");
            }

            _formatContext = formatContext;

            var streamInfoResult = ffmpeg.avformat_find_stream_info(_formatContext, null);
            if (streamInfoResult < 0)
            {
                ThrowIfCancelled(cancellationToken);
                throw new VideoFrameSourceException($"Reading stream information failed: {GetErrorDescription(streamInfoResult)}.");
            }

            var hasAudio = false;
            AVStream* videoStream = null;

            for (var i = 0u; i < _formatContext->nb_streams; i++)
            {
                var candidate = _formatContext->streams[i];

                if (candidate->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                    hasAudio = true;
                else if (candidate->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO && videoStream is null)
                    videoStream = candidate;
            }

            if (videoStream is null)
                throw new VideoFrameSourceException("The source has no video stream.");

            _videoStreamIndex = videoStream->index;

            var decoder = ffmpeg.avcodec_find_decoder(videoStream->codecpar->codec_id);
            if (decoder is null)
                throw new VideoFrameSourceException("No decoder is available for the source's video codec.");

            _codecContext = ffmpeg.avcodec_alloc_context3(decoder);
            ThrowIfError(ffmpeg.avcodec_parameters_to_context(_codecContext, videoStream->codecpar), "Copying codec parameters");
            ThrowIfError(ffmpeg.avcodec_open2(_codecContext, decoder, null), "Opening the decoder");

            var sourceWidth = _codecContext->width;
            var sourceHeight = _codecContext->height;
            (_scaledWidth, _scaledHeight) = VideoFrameScaling.ComputeScaledSize(sourceWidth, sourceHeight, _options.MaxWidth, _options.MaxHeight);
            _scaledFrameBuffer = new byte[_scaledWidth * _scaledHeight * 4];

            _swsContext = ffmpeg.sws_getContext(
                sourceWidth, sourceHeight, _codecContext->pix_fmt,
                _scaledWidth, _scaledHeight, AVPixelFormat.AV_PIX_FMT_BGRA,
                SwsBilinear, null, null, null);

            if (_swsContext is null)
                throw new VideoFrameSourceException("Creating the frame scaler failed.");

            _packet = ffmpeg.av_packet_alloc();
            _frame = ffmpeg.av_frame_alloc();

            // Comparing the stream's average and "lowest common denominator" frame rates is a
            // best-effort VFR heuristic only — see this field's own remarks. It never feeds into any
            // individual frame's own PresentationTimestamp/Duration, which always come from that
            // frame's own AVFrame.best_effort_timestamp/duration (see ScaleFrame).
            var isVariableFrameRate = videoStream->r_frame_rate.num != 0
                && videoStream->avg_frame_rate.num != 0
                && videoStream->r_frame_rate.den * videoStream->avg_frame_rate.num != videoStream->r_frame_rate.num * videoStream->avg_frame_rate.den;

            var durationTicks = _formatContext->duration > 0
                ? _formatContext->duration * TimeSpan.TicksPerSecond / ffmpeg.AV_TIME_BASE
                : 0;

            StreamInfo = new VideoStreamInfo
            {
                Width = _scaledWidth,
                Height = _scaledHeight,
                PixelFormat = VideoPixelFormat.Bgra32,
                IsVariableFrameRate = isVariableFrameRate,
                NominalFrameRate = videoStream->avg_frame_rate.den != 0 ? ffmpeg.av_q2d(videoStream->avg_frame_rate) : 0,
                Duration = TimeSpan.FromTicks(durationTicks),
                HasAudio = hasAudio
            };

            return StreamInfo;
        }

        private void Seek(TimeSpan position, CancellationToken cancellationToken)
        {
            _activeCancellationToken = cancellationToken;

            var stream = _formatContext->streams[_videoStreamIndex];
            var timestamp = (long)(position.TotalSeconds / ffmpeg.av_q2d(stream->time_base));

            var seekResult = ffmpeg.av_seek_frame(_formatContext, _videoStreamIndex, timestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
            if (seekResult < 0)
            {
                ThrowIfCancelled(cancellationToken);
                throw new VideoFrameSourceException($"Seeking failed: {GetErrorDescription(seekResult)}.");
            }

            ffmpeg.avcodec_flush_buffers(_codecContext);
        }

        // Ordinary, fully synchronous method — the only place this class actually drives the
        // read-packet / send-to-decoder / receive-frame loop. Returns null only at genuine end of
        // stream (after the decoder has been flushed and fully drained — #217's own "Drain FFmpeg
        // correctly" scope); any failure throws rather than returning null, so a caller never has to
        // guess which one happened.
        private DecodedVideoFrame? DecodeNextFrame(CancellationToken cancellationToken)
        {
            _activeCancellationToken = cancellationToken;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var receiveResult = ffmpeg.avcodec_receive_frame(_codecContext, _frame);
                if (receiveResult == 0)
                {
                    var decodedFrame = ScaleFrame(_frame);
                    ffmpeg.av_frame_unref(_frame);
                    return decodedFrame;
                }

                if (receiveResult == ffmpeg.AVERROR_EOF)
                    return null;

                if (receiveResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                    ThrowIfError(receiveResult, "Receiving a decoded frame");

                // The decoder needs another packet before it can produce another frame.
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var readResult = ffmpeg.av_read_frame(_formatContext, _packet);
                    if (readResult == ffmpeg.AVERROR_EOF)
                    {
                        // Signals end-of-stream to the decoder so it flushes any frames it was still
                        // buffering internally — those are drained by the next avcodec_receive_frame
                        // call(s) above before this method finally returns null. A repeat flush signal
                        // on a later call is harmless (FFmpeg documents it as a no-op AVERROR_EOF).
                        ffmpeg.avcodec_send_packet(_codecContext, null);
                        break;
                    }

                    ThrowIfError(readResult, "Reading a packet");

                    if (_packet->stream_index != _videoStreamIndex)
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

        private DecodedVideoFrame ScaleFrame(AVFrame* frame)
        {
            var sourceData = new byte*[8];
            var sourceLinesize = new int[8];
            for (var i = 0u; i < 8; i++)
            {
                sourceData[i] = frame->data[i];
                sourceLinesize[i] = frame->linesize[i];
            }

            var destinationData = new byte*[8];
            var destinationLinesize = new int[8];
            var buffer = _scaledFrameBuffer!;

            fixed (byte* destination = buffer)
            {
                destinationData[0] = destination;
                destinationLinesize[0] = _scaledWidth * 4;

                ThrowIfError(
                    ffmpeg.sws_scale(_swsContext, sourceData, sourceLinesize, 0, _codecContext->height, destinationData, destinationLinesize),
                    "Scaling a decoded frame");
            }

            var stream = _formatContext->streams[_videoStreamIndex];
            var timeBase = stream->time_base;

            var presentationTimestamp = TimeSpan.FromSeconds(frame->best_effort_timestamp * ffmpeg.av_q2d(timeBase));
            var duration = frame->duration > 0
                ? TimeSpan.FromSeconds(frame->duration * ffmpeg.av_q2d(timeBase))
                : TimeSpan.Zero;

            return new DecodedVideoFrame(presentationTimestamp, duration, _scaledWidth, _scaledHeight, VideoPixelFormat.Bgra32, buffer);
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
