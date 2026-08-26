using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio
{
    /// <summary>Configuration for <see cref="FfmpegAudioFrameSource"/>. See <see cref="FfmpegVideoFrameSource"/>'s own remarks for the native FFmpeg shared libraries <see cref="RootPath"/> must point at.</summary>
    public sealed class FfmpegAudioFrameSourceOptions
    {
        /// <summary>Directory containing the native FFmpeg shared libraries this platform needs. Left <see langword="null"/>, the default OS library search path is used instead. See <see cref="FfmpegVideoFrameSource"/>'s own remarks.</summary>
        public string? RootPath { get; set; }

        /// <summary>
        /// Output sample rate every <see cref="DecodedAudioFrame"/> this source yields is resampled to,
        /// regardless of the source's own native rate — one of Opus's own supported rates (8000, 12000,
        /// 16000, 24000, 48000 Hz), since <see cref="AudioFrameEncoder"/> always encodes whatever this
        /// source yields. Default: 48000 (Opus's own highest-quality native rate).
        /// </summary>
        public int TargetSampleRate { get; set; } = 48000;

        /// <summary>Maximum output channel count — a source with more channels is downmixed to this many. Must be 1 (mono) or 2 (stereo). Default: 2.</summary>
        public int MaxChannels { get; set; } = 2;
    }
}
