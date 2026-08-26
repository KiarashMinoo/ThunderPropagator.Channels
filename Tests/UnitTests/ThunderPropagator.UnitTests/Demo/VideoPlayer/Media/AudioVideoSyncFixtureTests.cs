using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Issue #224's own AC: "Deterministic server sync tests and an opt-in A/V fixture test pass." This
    /// is the opt-in half — real FFmpeg decode/encode of an actual media file with both an audio and a
    /// video track, verifying the two tracks' own timestamps stay coherent with each other. It never runs
    /// in this repo's own CI/dev environment (no native FFmpeg libraries, no checked-in media fixture —
    /// exactly the same limitation <see cref="FfmpegVideoFrameSourceTests"/>'s own remarks describe, and
    /// the same reasoning #237 already established for the video-only equivalent of this test) — set
    /// <see cref="FixtureEnvironmentVariable"/> to a local audio+video file's own path to opt in wherever
    /// FFmpeg is actually available.
    /// </summary>
    public sealed class AudioVideoSyncFixtureTests
    {
        /// <summary>Environment variable naming a local media file (with both an audio and a video track) to run this suite against. Unset or pointing at a non-existent file: every test here is a no-op pass.</summary>
        public const string FixtureEnvironmentVariable = "THUNDERPROPAGATOR_VIDEOPLAYER_AV_FIXTURE";

        private static string? GetFixturePathOrNull()
        {
            var path = Environment.GetEnvironmentVariable(FixtureEnvironmentVariable);
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? path : null;
        }

        [Fact]
        public async Task DecodingBothTracks_YieldsPresentationTimestampsThatStartTogether_AndEncodesValidOpusPackets()
        {
            var fixturePath = GetFixturePathOrNull();
            if (fixturePath is null)
                return; // opted out — see this type's own remarks.

            var mediaSource = new VideoSource { Location = fixturePath };

            await using var videoSource = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions());
            var videoStreamInfo = await videoSource.OpenAsync(mediaSource);

            await using var audioSource = new FfmpegAudioFrameSource(new FfmpegAudioFrameSourceOptions());

            if (!videoStreamInfo.HasAudio)
                return; // this particular fixture has no audio track to synchronize against — nothing to prove here.

            var audioStreamInfo = await audioSource.OpenAsync(mediaSource);
            using var encoder = new AudioFrameEncoder(audioStreamInfo.SampleRate, audioStreamInfo.Channels);

            TimeSpan? firstVideoPts = null;
            await foreach (var frame in videoSource.ReadFramesAsync(TimeSpan.Zero))
            {
                firstVideoPts = frame.PresentationTimestamp;
                frame.Dispose();
                break;
            }

            TimeSpan? firstAudioPts = null;
            var encodedAnyPacket = false;
            await foreach (var frame in audioSource.ReadFramesAsync(TimeSpan.Zero))
            {
                firstAudioPts ??= frame.PresentationTimestamp;

                IReadOnlyList<EncodedAudioChunk> chunks;
                using (frame)
                    chunks = encoder.Encode(frame);

                foreach (var chunk in chunks)
                {
                    Assert.NotEmpty(chunk.Payload.ToArray());
                    encodedAnyPacket = true;
                }

                if (encodedAnyPacket)
                    break;
            }

            Assert.NotNull(firstVideoPts);
            Assert.NotNull(firstAudioPts);

            // Both tracks are expected to start at (or extremely near) the beginning of the same media
            // timeline — a gap here would indicate the two decoders disagree about where "the start" is,
            // which is exactly what #224's own AC ("audio and video packets share ... synchronized
            // timestamps") requires they never do.
            Assert.True(Math.Abs((firstVideoPts!.Value - firstAudioPts!.Value).TotalSeconds) < 1.0);
        }
    }
}
