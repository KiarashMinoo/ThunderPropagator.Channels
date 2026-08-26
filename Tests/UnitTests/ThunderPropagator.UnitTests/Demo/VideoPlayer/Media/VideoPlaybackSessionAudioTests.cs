using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Demo.VideoPlayer;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Issue #224's own ACs at the session level: audio and video packets share one session epoch and
    /// synchronized timestamps, muted/video-only sessions run with no audio resources at all, a missing
    /// or failing audio track never faults an otherwise-healthy video session, and a seek/select resets
    /// audio exactly as coherently as it already resets video.
    /// </summary>
    public sealed class VideoPlaybackSessionAudioTests
    {
        private static readonly VideoSource TestSource = new() { Location = "synthetic://test" };

        private static VideoPlaybackSessionOptions FastOptions() => new()
        {
            PlaybackRate = 100_000,
            PollInterval = TimeSpan.FromMilliseconds(2)
        };

        private static ReadOnlyMemory<byte> PassthroughVideoEncode(DecodedVideoFrame frame) => frame.Data;

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Condition was not met in time.");

                await Task.Delay(2);
            }
        }

        [Fact]
        public async Task Session_WithoutAnAudioSourceFactory_NeverPublishesAudio()
        {
            await using var session = new VideoPlaybackSession("s1", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughVideoEncode);
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            Assert.False(session.TryDequeueAudio("viewer", out _));
        }

        [Fact]
        public async Task Session_WithEnableAudioFalse_NeverPublishesAudio_EvenWithAFactorySupplied()
        {
            var options = FastOptions() with { EnableAudio = false };
            await using var session = new VideoPlaybackSession(
                "s2", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughVideoEncode,
                audioSourceFactory: () => new SyntheticAudioFrameSource(), audioEncoderFactory: (_, _, encoding) => new PassthroughAudioEncoder(encoding));
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            Assert.False(session.TryDequeueAudio("viewer", out _));
        }

        [Fact]
        public async Task Session_WithAudioEnabled_PublishesBothTracks_SharingTheSameEpoch()
        {
            await using var session = new VideoPlaybackSession(
                "s3", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughVideoEncode,
                audioSourceFactory: () => new SyntheticAudioFrameSource(), audioEncoderFactory: (_, _, encoding) => new PassthroughAudioEncoder(encoding));
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            var videoPackets = new List<VideoFramePacket>();
            while (session.TryDequeue("viewer", out var packet))
                videoPackets.Add(packet!);

            var audioPackets = new List<AudioFramePacket>();
            while (session.TryDequeueAudio("viewer", out var packet))
                audioPackets.Add(packet!);

            Assert.NotEmpty(videoPackets);
            Assert.NotEmpty(audioPackets);
            Assert.All(videoPackets, p => Assert.Equal(session.Epoch, p.Epoch));
            Assert.All(audioPackets, p => Assert.Equal(session.Epoch, p.Epoch));
            Assert.All(videoPackets.Zip(videoPackets.Skip(1)), pair => Assert.True(pair.Second.FrameNumber > pair.First.FrameNumber));
            Assert.All(audioPackets.Zip(audioPackets.Skip(1)), pair => Assert.True(pair.Second.PacketNumber > pair.First.PacketNumber));
        }

        [Fact]
        public async Task Session_WhenAudioSourceHasNoAudioTrack_RunsVideoOnly_WithoutFaultingTheSession()
        {
            await using var session = new VideoPlaybackSession(
                "s4", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughVideoEncode,
                audioSourceFactory: () => new FakeAudioFrameSource(FakeAudioFrameSource.FailureMode.OpenThrowsNoAudioTrack), audioEncoderFactory: (_, _, encoding) => new PassthroughAudioEncoder(encoding));
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            Assert.Null(session.Fault);
            Assert.True(session.TryDequeue("viewer", out _), "video must still have played normally");
            Assert.False(session.TryDequeueAudio("viewer", out _));
        }

        [Fact]
        public async Task Session_WhenAudioFaultsAtRuntime_VideoIsUnaffected_AndSessionDoesNotFault()
        {
            await using var session = new VideoPlaybackSession(
                "s5", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughVideoEncode,
                audioSourceFactory: () => new FakeAudioFrameSource(FakeAudioFrameSource.FailureMode.ThrowsWhileReading), audioEncoderFactory: (_, _, encoding) => new PassthroughAudioEncoder(encoding));
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            Assert.Null(session.Fault);
            Assert.Equal(PlayState.Ended, session.State);
            Assert.True(session.TryDequeue("viewer", out _), "video must still have played normally despite the audio-side fault");
        }

        [Fact]
        public async Task SelectAsync_CalledAgain_NeverPublishesAudioFromTheSupersededEpoch()
        {
            await using var session = new VideoPlaybackSession(
                "s6", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughVideoEncode,
                audioSourceFactory: () => new SyntheticAudioFrameSource(), audioEncoderFactory: (_, _, encoding) => new PassthroughAudioEncoder(encoding));
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            var firstEpoch = session.Epoch;

            // Generation 1 legitimately published a bit of audio during its own (brief, FastOptions-
            // accelerated) life before the switch below — the AC under test is "no MORE epoch-1 audio is
            // published after the switch," not "none was ever published," so discard that history first
            // rather than asserting on it.
            while (session.TryDequeueAudio("viewer", out _)) { }

            await session.SelectAsync(TestSource);
            var secondEpoch = session.Epoch;

            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            var audioPackets = new List<AudioFramePacket>();
            while (session.TryDequeueAudio("viewer", out var packet))
                audioPackets.Add(packet!);

            Assert.True(secondEpoch > firstEpoch);
            Assert.NotEmpty(audioPackets);
            Assert.All(audioPackets, p => Assert.Equal(secondEpoch, p.Epoch));
        }

        [Fact]
        public async Task Join_BootstrapsAudioAlongsideVideo()
        {
            await using var session = new VideoPlaybackSession(
                "s7", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughVideoEncode,
                audioSourceFactory: () => new SyntheticAudioFrameSource(), audioEncoderFactory: (_, _, encoding) => new PassthroughAudioEncoder(encoding));

            session.Subscribe("probe");
            await session.SelectAsync(TestSource);
            // Wait for BOTH tracks to have started publishing — video and audio decode/publish at
            // different native chunk rates, so one can easily be flowing before the other; waiting on
            // audio alone left a real window where Join's own video bootstrap assertion below could fire
            // before any video frame had published yet. The flags below accumulate ACROSS polls (each
            // poll only drains whatever is newly available since the last one), not just within one.
            var sawVideo = false;
            var sawAudio = false;
            await WaitUntilAsync(() =>
            {
                while (session.TryDequeue("probe", out _))
                    sawVideo = true;

                while (session.TryDequeueAudio("probe", out _))
                    sawAudio = true;

                return sawVideo && sawAudio;
            }, TimeSpan.FromSeconds(5));

            var snapshot = session.Join("lateViewer");

            Assert.True(snapshot.HasBootstrapFrame);
            Assert.True(session.TryDequeueAudio("lateViewer", out var bootstrapAudio), "a late joiner must also receive an audio bootstrap packet once audio has started publishing");
            Assert.Equal(snapshot.Epoch, bootstrapAudio!.Epoch);
        }

        [Fact]
        public async Task Session_AutoDetectsAacEncoding_WhenTheSourceIsAlreadyAac()
        {
            await using var session = new VideoPlaybackSession(
                "s8", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughVideoEncode,
                audioSourceFactory: () => new SyntheticAudioFrameSource { SourceCodecName = "aac" }, audioEncoderFactory: (_, _, encoding) => new PassthroughAudioEncoder(encoding));
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.AudioEncoding is not null, TimeSpan.FromSeconds(5));

            Assert.Equal(AudioFramePacketEncoding.Aac, session.AudioEncoding);

            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));
            Assert.True(session.TryDequeueAudio("viewer", out var packet));
            Assert.Equal(AudioFramePacketEncoding.Aac, packet!.Encoding);
        }

        [Fact]
        public async Task Session_AutoDetectsOpusEncoding_WhenTheSourceIsNotAac()
        {
            await using var session = new VideoPlaybackSession(
                "s9", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughVideoEncode,
                audioSourceFactory: () => new SyntheticAudioFrameSource { SourceCodecName = "mp3" }, audioEncoderFactory: (_, _, encoding) => new PassthroughAudioEncoder(encoding));
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.AudioEncoding is not null, TimeSpan.FromSeconds(5));

            Assert.Equal(AudioFramePacketEncoding.Opus, session.AudioEncoding);
        }

        [Fact]
        public async Task Session_ExplicitAudioEncodingOption_OverridesAutoDetection()
        {
            var options = FastOptions() with { AudioEncoding = AudioFramePacketEncoding.Opus };
            await using var session = new VideoPlaybackSession(
                "s10", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughVideoEncode,
                // The source itself is AAC, which would auto-detect to Aac — the explicit option must win regardless.
                audioSourceFactory: () => new SyntheticAudioFrameSource { SourceCodecName = "aac" }, audioEncoderFactory: (_, _, encoding) => new PassthroughAudioEncoder(encoding));
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.AudioEncoding is not null, TimeSpan.FromSeconds(5));

            Assert.Equal(AudioFramePacketEncoding.Opus, session.AudioEncoding);
        }

        [Fact]
        public async Task AudioEncoding_IsNull_UntilAudioActuallyActivates()
        {
            await using var session = new VideoPlaybackSession("s11", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughVideoEncode);

            Assert.Null(session.AudioEncoding);

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            Assert.Null(session.AudioEncoding);
        }

        private sealed class FakeAudioFrameSource : IAudioFrameSource
        {
            public enum FailureMode
            {
                OpenThrowsNoAudioTrack,
                ThrowsWhileReading
            }

            private readonly FailureMode _mode;

            public FakeAudioFrameSource(FailureMode mode) => _mode = mode;

            public AudioStreamInfo? StreamInfo { get; private set; }

            public Task<AudioStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
            {
                if (_mode == FailureMode.OpenThrowsNoAudioTrack)
                    throw new VideoFrameSourceException("The source has no audio stream.");

                var info = new AudioStreamInfo { SampleRate = 48_000, Channels = 2, SampleFormat = AudioSampleFormat.Float32Interleaved };
                StreamInfo = info;
                return Task.FromResult(info);
            }

            public async IAsyncEnumerable<DecodedAudioFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                throw new VideoFrameSourceException("synthetic audio decode failure");
#pragma warning disable CS0162
                yield break;
#pragma warning restore CS0162
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
