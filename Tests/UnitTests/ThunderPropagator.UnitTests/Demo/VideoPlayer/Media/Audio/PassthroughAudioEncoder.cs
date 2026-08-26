using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Audio
{
    /// <summary>
    /// A deterministic, dependency-free <see cref="IAudioEncoder"/> — turns each <see cref="DecodedAudioFrame"/>
    /// straight into one <see cref="EncodedAudioChunk"/> carrying its own raw (unencoded) bytes, with no
    /// accumulation of its own (<see cref="FrameSize"/> is 1), so a <see cref="VideoPlaybackSession"/>'s
    /// own audio wiring/timing logic is testable without any real Opus/native FFmpeg dependency.
    /// </summary>
    public sealed class PassthroughAudioEncoder(AudioFramePacketEncoding encoding = AudioFramePacketEncoding.Opus) : IAudioEncoder
    {
        public AudioFramePacketEncoding Encoding => encoding;

        public int FrameSize => 1;

        public IReadOnlyList<EncodedAudioChunk> Encode(DecodedAudioFrame frame) =>
            [new EncodedAudioChunk(frame.Data, frame.PresentationTimestamp, frame.Duration)];

        public IReadOnlyList<EncodedAudioChunk> Flush() => [];

        public void Dispose()
        {
        }
    }
}
