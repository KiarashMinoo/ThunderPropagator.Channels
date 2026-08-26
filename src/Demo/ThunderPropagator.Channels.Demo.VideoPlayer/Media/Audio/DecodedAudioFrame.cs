using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// One raw, not-yet-encoded chunk of decoded audio samples from an <see cref="IAudioFrameSource"/> —
    /// the audio-side counterpart to <see cref="DecodedVideoFrame"/>, sharing its own remarks on buffer
    /// ownership and disposal idempotency verbatim: <see cref="Data"/> is not guaranteed to be this
    /// instance's private copy (a decoder may reuse its own pooled buffer across calls) and is only valid
    /// until <see cref="Dispose"/> is called; <see cref="Dispose"/> is safe to call more than once.
    /// </summary>
    public sealed class DecodedAudioFrame : IDisposable
    {
        private Action? _onDispose;

        public DecodedAudioFrame(TimeSpan presentationTimestamp, TimeSpan duration, int sampleRate, int channels, AudioSampleFormat sampleFormat, ReadOnlyMemory<byte> data, Action? onDispose = null)
        {
            PresentationTimestamp = presentationTimestamp;
            Duration = duration;
            SampleRate = sampleRate;
            Channels = channels;
            SampleFormat = sampleFormat;
            Data = data;
            _onDispose = onDispose;
        }

        /// <summary>This frame's presentation timestamp, exactly as reported by the source — never reconstructed from an assumed sample count.</summary>
        public TimeSpan PresentationTimestamp { get; }

        /// <summary>How long this chunk of audio plays before the next one is due, exactly as reported by the source.</summary>
        public TimeSpan Duration { get; }

        /// <summary>Samples per second.</summary>
        public int SampleRate { get; }

        /// <summary>Channel count (1 = mono, 2 = stereo).</summary>
        public int Channels { get; }

        /// <summary>This frame's raw sample layout.</summary>
        public AudioSampleFormat SampleFormat { get; }

        /// <summary>The raw sample buffer. See this type's own remarks on ownership and lifetime.</summary>
        public ReadOnlyMemory<byte> Data { get; }

        /// <summary>Releases this frame's underlying buffer back to whatever produced it, if anything needs to. Idempotent.</summary>
        public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
    }
}
