using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// A deterministic, dependency-free <see cref="IAudioFrameSource"/> — the audio-side counterpart to
    /// <see cref="SyntheticVideoFrameSource"/>, used to exercise <see cref="AudioFrameSourceContractTests"/>
    /// and #224's own deterministic sync tests without any real media or native FFmpeg dependency.
    /// </summary>
    public sealed class SyntheticAudioFrameSource : IAudioFrameSource
    {
        public const int SampleRate = 48_000;
        public const int Channels = 2;

        // Deliberately irregular, mirroring SyntheticVideoFrameSource's own reasoning: no single constant
        // chunk size could reproduce these durations by dividing an index by it.
        public static readonly IReadOnlyList<TimeSpan> ChunkDurations =
        [
            TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(23), TimeSpan.FromMilliseconds(20),
            TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(23),
            TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(23),
            TimeSpan.FromMilliseconds(20)
        ];

        private bool _opened;
        private int _disposedFrameCount;

        /// <summary>How many frames this instance has produced have since had <see cref="DecodedAudioFrame.Dispose"/> called on them exactly once.</summary>
        public int DisposedFrameCount => Volatile.Read(ref _disposedFrameCount);

        /// <summary>The <see cref="AudioStreamInfo.SourceCodecName"/> this instance reports on <see cref="OpenAsync"/> — settable so tests can exercise a <see cref="VideoPlaybackSession"/>'s own auto-detection without a real source. Default: <c>"synthetic"</c>.</summary>
        public string SourceCodecName { get; set; } = "synthetic";

        public AudioStreamInfo? StreamInfo { get; private set; }

        public Task<AudioStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            cancellationToken.ThrowIfCancellationRequested();

            StreamInfo = new AudioStreamInfo
            {
                SampleRate = SampleRate,
                Channels = Channels,
                SampleFormat = AudioSampleFormat.Float32Interleaved,
                Duration = TimeSpan.FromTicks(ChunkDurations.Sum(d => d.Ticks)),
                SourceCodecName = SourceCodecName
            };
            _opened = true;

            return Task.FromResult(StreamInfo);
        }

        public async IAsyncEnumerable<DecodedAudioFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!_opened)
                throw new InvalidOperationException($"{nameof(ReadFramesAsync)} was called before {nameof(OpenAsync)} completed.");

            cancellationToken.ThrowIfCancellationRequested();

            var pts = TimeSpan.Zero;
            for (var chunkIndex = 0; chunkIndex < ChunkDurations.Count; chunkIndex++)
            {
                var duration = ChunkDurations[chunkIndex];
                var chunkPts = pts;
                pts += duration;

                if (chunkPts + duration > startPosition)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                    yield return CreateFrame(chunkIndex, chunkPts, duration);
                }
            }
        }

        private DecodedAudioFrame CreateFrame(int chunkIndex, TimeSpan presentationTimestamp, TimeSpan duration)
        {
            var sampleCount = (int)(duration.TotalSeconds * SampleRate);
            var data = new byte[sampleCount * Channels * sizeof(float)];
            Array.Fill(data, (byte)chunkIndex);

            return new DecodedAudioFrame(presentationTimestamp, duration, SampleRate, Channels, AudioSampleFormat.Float32Interleaved, data,
                onDispose: () => Interlocked.Increment(ref _disposedFrameCount));
        }

        public ValueTask DisposeAsync()
        {
            _opened = false;
            return ValueTask.CompletedTask;
        }
    }
}
