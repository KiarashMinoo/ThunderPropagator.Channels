using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// A deterministic, dependency-free <see cref="IVideoFrameSource"/> used to prove the abstraction
    /// itself is sound before any real decoder (#217) exists, and to exercise
    /// <see cref="VideoFrameSourceContractTests"/> — #216's own AC: "A synthetic implementation can
    /// produce deterministic VFR frames."
    /// </summary>
    public sealed class SyntheticVideoFrameSource : IVideoFrameSource
    {
        public const int FrameWidth = 4;
        public const int FrameHeight = 4;

        // Deliberately irregular — no single constant frame rate could reproduce these durations by
        // dividing a frame index by it, so any code deriving PTS this way would visibly diverge from
        // what this source actually reports. #216's own AC: "PTS and duration are not reconstructed
        // from an assumed FPS."
        public static readonly IReadOnlyList<TimeSpan> FrameDurations =
        [
            TimeSpan.FromMilliseconds(33), TimeSpan.FromMilliseconds(41), TimeSpan.FromMilliseconds(33),
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(33), TimeSpan.FromMilliseconds(41),
            TimeSpan.FromMilliseconds(33), TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(41),
            TimeSpan.FromMilliseconds(33)
        ];

        private bool _opened;
        private int _disposedFrameCount;

        /// <summary>How many frames this instance has produced have since had <see cref="DecodedVideoFrame.Dispose"/> called on them exactly once.</summary>
        public int DisposedFrameCount => Volatile.Read(ref _disposedFrameCount);

        public VideoStreamInfo? StreamInfo { get; private set; }

        public Task<VideoStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            cancellationToken.ThrowIfCancellationRequested();

            StreamInfo = new VideoStreamInfo
            {
                Width = FrameWidth,
                Height = FrameHeight,
                PixelFormat = VideoPixelFormat.Rgb24,
                IsVariableFrameRate = true,
                NominalFrameRate = 30,
                Duration = TimeSpan.FromTicks(FrameDurations.Sum(duration => duration.Ticks))
            };
            _opened = true;

            return Task.FromResult(StreamInfo);
        }

        public async IAsyncEnumerable<DecodedVideoFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!_opened)
                throw new InvalidOperationException($"{nameof(ReadFramesAsync)} was called before {nameof(OpenAsync)} completed.");

            cancellationToken.ThrowIfCancellationRequested();

            var pts = TimeSpan.Zero;
            for (var frameIndex = 0; frameIndex < FrameDurations.Count; frameIndex++)
            {
                var duration = FrameDurations[frameIndex];
                var framePts = pts;
                pts += duration;

                if (framePts + duration > startPosition)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield(); // exercises real asynchrony rather than a synchronous fast-path
                    yield return CreateFrame(frameIndex, framePts, duration);
                }
            }
        }

        private DecodedVideoFrame CreateFrame(int frameIndex, TimeSpan presentationTimestamp, TimeSpan duration)
        {
            var data = new byte[FrameWidth * FrameHeight * 3];
            Array.Fill(data, (byte)frameIndex);

            return new DecodedVideoFrame(presentationTimestamp, duration, FrameWidth, FrameHeight, VideoPixelFormat.Rgb24, data,
                onDispose: () => Interlocked.Increment(ref _disposedFrameCount));
        }

        public ValueTask DisposeAsync()
        {
            _opened = false;
            return ValueTask.CompletedTask;
        }
    }
}
