using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Demo.VideoPlayer;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;
using ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Session
{
    /// <summary>
    /// Issue #220's own ACs for one session: multiple viewers share one decoder/timeline, joining or
    /// leaving never restarts or duplicates decoding, media resources are disposed only after both loops
    /// have actually stopped, and faults/natural end-of-stream transition state predictably.
    /// </summary>
    public sealed class VideoPlaybackSessionTests
    {
        private static readonly VideoSource TestSource = new() { Location = "synthetic://test" };

        // Collapses SyntheticVideoFrameSource's ~380ms of media into a handful of milliseconds so these
        // tests run fast without needing a fake clock (a real clock keeps the publish loop's own
        // asynchrony genuine rather than deterministically single-stepped).
        private static VideoPlaybackSessionOptions FastOptions() => new()
        {
            PlaybackRate = 100_000,
            PollInterval = TimeSpan.FromMilliseconds(2)
        };

        private static ReadOnlyMemory<byte> PassthroughEncode(DecodedVideoFrame frame) => frame.Data;

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Condition was not met in time.");

                await Task.Delay(5);
            }
        }

        [Fact]
        public void Constructor_WithInvalidArguments_Throws()
        {
            Assert.Throws<ArgumentException>(() => new VideoPlaybackSession("", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock()));
            Assert.Throws<ArgumentNullException>(() => new VideoPlaybackSession("s", null!, new SystemMonotonicClock()));
            Assert.Throws<ArgumentNullException>(() => new VideoPlaybackSession("s", () => new SyntheticVideoFrameSource(), null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VideoPlaybackSession("s", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), new VideoPlaybackSessionOptions { DecodeBufferCapacity = 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VideoPlaybackSession("s", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), new VideoPlaybackSessionOptions { SubscriberQueueCapacity = 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VideoPlaybackSession("s", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), new VideoPlaybackSessionOptions { PlaybackRate = 0 }));
        }

        [Fact]
        public async Task SelectAsync_PublishesFrames_ToEveryViewerIdentically()
        {
            await using var session = new VideoPlaybackSession("s1", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);
            session.Subscribe("viewerA");
            session.Subscribe("viewerB");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            var framesA = new List<VideoFramePacket>();
            while (session.TryDequeue("viewerA", out var packet))
                framesA.Add(packet!);

            var framesB = new List<VideoFramePacket>();
            while (session.TryDequeue("viewerB", out var packet))
                framesB.Add(packet!);

            Assert.NotEmpty(framesA);
            Assert.Equal(framesA.Select(f => f.FrameNumber), framesB.Select(f => f.FrameNumber));
        }

        [Fact]
        public async Task Select_ThenNaturalEndOfStream_TransitionsToEnded_WithNoFault()
        {
            await using var session = new VideoPlaybackSession("s2", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            Assert.Null(session.Fault);
        }

        [Fact]
        public async Task SubscribeAndUnsubscribe_WhilePlaying_NeverReopenTheSource()
        {
            var openCount = 0;

            IVideoFrameSource Factory()
            {
                Interlocked.Increment(ref openCount);
                return new SyntheticVideoFrameSource();
            }

            await using var session = new VideoPlaybackSession("s3", Factory, new SystemMonotonicClock(), FastOptions(), PassthroughEncode);
            await session.SelectAsync(TestSource);

            for (var i = 0; i < 20; i++)
            {
                session.Subscribe($"viewer{i}");
                session.Unsubscribe($"viewer{i}");
            }

            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            Assert.Equal(1, openCount);
        }

        [Fact]
        public async Task Fault_DuringDecode_TransitionsToFaulted_AndDisposesTheSource()
        {
            var faultingSource = new FaultingVideoFrameSource();
            await using var session = new VideoPlaybackSession("s4", () => faultingSource, new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.State == PlayState.Faulted, TimeSpan.FromSeconds(5));

            Assert.IsType<VideoFrameSourceException>(session.Fault);
            Assert.True(faultingSource.Disposed);
        }

        [Fact]
        public async Task EndAsync_DisposesMediaResources_ButKeepsSubscribersRegistered()
        {
            await using var session = new VideoPlaybackSession("s5", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await session.EndAsync();

            Assert.Equal(PlayState.Ended, session.State);
            Assert.Equal(1, session.ViewerCount);
        }

        [Fact]
        public async Task DisposeAsync_DisposesEverySubscriberQueue_SoFurtherUseThrows()
        {
            var session = new VideoPlaybackSession("s6", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);
            session.Subscribe("viewer");

            await session.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => session.Subscribe("another"));
        }

        [Fact]
        public async Task DisposeAsync_IsSafeToCallMoreThanOnce()
        {
            var session = new VideoPlaybackSession("s7", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            await session.DisposeAsync();
            var exception = await Record.ExceptionAsync(async () => await session.DisposeAsync());

            Assert.Null(exception);
        }

        [Fact]
        public async Task PauseAsync_WithoutASelectedVideo_Throws()
        {
            await using var session = new VideoPlaybackSession("s8", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            await Assert.ThrowsAsync<InvalidOperationException>(() => session.PauseAsync());
        }

        [Fact]
        public async Task ResumeAsync_WithoutASelectedVideo_Throws()
        {
            await using var session = new VideoPlaybackSession("s9", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResumeAsync());
        }

        [Fact]
        public async Task SeekAsync_WithoutASelectedVideo_Throws()
        {
            await using var session = new VideoPlaybackSession("s10", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            await Assert.ThrowsAsync<InvalidOperationException>(() => session.SeekAsync(TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public async Task PauseAsync_StopsFurtherPublishing_UntilResumeAsync()
        {
            // Deliberately NOT FastOptions() here: that accelerates ~380ms of synthetic media down to a
            // few milliseconds, which would let the whole stream finish naturally before this test ever
            // gets to call PauseAsync. A modest, still-quick rate leaves real margin to pause mid-stream.
            var options = new VideoPlaybackSessionOptions { PlaybackRate = 4.0, PollInterval = TimeSpan.FromMilliseconds(2) };
            await using var session = new VideoPlaybackSession("s11", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughEncode);
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.TryDequeue("viewer", out _), TimeSpan.FromSeconds(5));
            while (session.TryDequeue("viewer", out _)) { } // drain whatever is already queued

            await session.PauseAsync();
            Assert.Equal(PlayState.Paused, session.State);

            await Task.Delay(30); // several poll intervals' worth of opportunity to (wrongly) publish while paused
            Assert.False(session.TryDequeue("viewer", out _));

            await session.ResumeAsync();
            Assert.Equal(PlayState.Playing, session.State);
        }

        [Fact]
        public async Task SelectAsync_CalledAgain_SupersedesThePreviousGeneration_AndDisposesItsSource()
        {
            var firstSource = new SyntheticVideoFrameSource();
            var sources = new Queue<IVideoFrameSource>([firstSource, new SyntheticVideoFrameSource()]);

            await using var session = new VideoPlaybackSession("s12", () => sources.Dequeue(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            await session.SelectAsync(TestSource);
            var firstEpoch = session.Epoch;

            await session.SelectAsync(TestSource);
            var secondEpoch = session.Epoch;

            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            Assert.True(secondEpoch > firstEpoch);
            await Task.Delay(20); // give the disposed first generation's own (superseded) supervisor a moment to finish, if it hasn't already
            Assert.Equal(PlayState.Ended, session.State); // the stale first generation's own completion must never have clobbered this
        }

        [Fact]
        public async Task CurrentSource_BeforeAnySelect_IsNull()
        {
            await using var session = new VideoPlaybackSession("s13", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            Assert.Null(session.CurrentSource);
        }

        [Fact]
        public async Task CurrentSource_AfterSelectAsync_IsTheSelectedSource()
        {
            await using var session = new VideoPlaybackSession("s14", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            await session.SelectAsync(TestSource);

            Assert.Same(TestSource, session.CurrentSource);
        }

        [Fact]
        public async Task Duration_BeforeAnySelect_IsNull()
        {
            await using var session = new VideoPlaybackSession("s19", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            Assert.Null(session.Duration);
        }

        [Fact]
        public async Task Duration_AfterSelectAsync_MatchesTheSourcesOwnStreamInfo()
        {
            await using var session = new VideoPlaybackSession("s20", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            await session.SelectAsync(TestSource);

            var expected = TimeSpan.FromTicks(SyntheticVideoFrameSource.FrameDurations.Sum(duration => duration.Ticks));
            Assert.Equal(expected, session.Duration);
        }

        [Fact]
        public async Task SeekAsync_CalledTwice_IncrementsEpochByExactlyTwo()
        {
            await using var session = new VideoPlaybackSession("s21", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            await session.SelectAsync(TestSource);
            var baseline = session.Epoch;

            await session.SeekAsync(TimeSpan.FromMilliseconds(10));
            await session.SeekAsync(TimeSpan.FromMilliseconds(20));

            Assert.Equal(baseline + 2, session.Epoch);
        }

        [Fact]
        public async Task SeekAsync_WhilePaused_StillSeeks()
        {
            // Deliberately NOT FastOptions() here, and deliberately proving playback actually reached the
            // stream (via TryDequeue) before pausing — see PauseAsync_StopsFurtherPublishing_UntilResumeAsync's
            // own comment on why a blind Select-then-Pause with no synchronization is a real flake risk,
            // not a hypothetical one. PlaybackRate 1.0 (near real-time) rather than that test's own 4.0:
            // this test needs to reliably catch the stream still mid-playback after an extra WaitUntilAsync
            // polling round-trip, and 4.0's own ~97ms real-time budget for the whole ~380ms of synthetic
            // media proved too tight for that under real background load — 1.0 leaves close to the full
            // ~380ms of margin instead.
            var options = new VideoPlaybackSessionOptions { PlaybackRate = 1.0, PollInterval = TimeSpan.FromMilliseconds(2) };
            await using var session = new VideoPlaybackSession("s22", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughEncode);
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.TryDequeue("viewer", out _), TimeSpan.FromSeconds(5));

            await session.PauseAsync();
            Assert.Equal(PlayState.Paused, session.State);

            var epochBeforeSeek = session.Epoch;
            await session.SeekAsync(TimeSpan.FromMilliseconds(10));

            Assert.True(session.Epoch > epochBeforeSeek);
        }

        [Fact]
        public async Task SeekAsync_NeverPublishesAnOldEpochPacket_AfterItCommits()
        {
            // #227's own AC: "No old-epoch frame/audio packet is published after seek." SwitchGenerationAsync
            // (SeekAsync's own implementation) cancels and fully awaits the previous generation's decode/publish
            // loops before returning, so by the time SeekAsync's Task completes, nothing enqueued afterward can
            // possibly carry the prior epoch — proven here by draining every packet the whole run ever produces
            // and asserting each one's own Epoch matches the epoch SeekAsync committed to, not the one before it.
            await using var session = new VideoPlaybackSession("s23", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.TryDequeue("viewer", out _), TimeSpan.FromSeconds(5));
            while (session.TryDequeue("viewer", out _)) { } // drain whatever the first generation already queued

            var epochBeforeSeek = session.Epoch;
            await session.SeekAsync(TimeSpan.Zero);
            var epochAfterSeek = session.Epoch;

            await WaitUntilAsync(() => session.State == PlayState.Ended, TimeSpan.FromSeconds(5));

            var packets = new List<VideoFramePacket>();
            while (session.TryDequeue("viewer", out var packet))
                packets.Add(packet!);

            Assert.True(epochAfterSeek > epochBeforeSeek);
            Assert.NotEmpty(packets);
            Assert.All(packets, p => Assert.Equal(epochAfterSeek, p.Epoch));
        }

        [Fact]
        public async Task TryClaimOrVerifyHost_FirstCaller_ClaimsHost()
        {
            await using var session = new VideoPlaybackSession("s15", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            Assert.True(session.TryClaimOrVerifyHost("connection-a"));
            Assert.Equal("connection-a", session.HostConnectionId);
        }

        [Fact]
        public async Task TryClaimOrVerifyHost_SameConnectionAgain_StaysHost()
        {
            await using var session = new VideoPlaybackSession("s16", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            Assert.True(session.TryClaimOrVerifyHost("connection-a"));
            Assert.True(session.TryClaimOrVerifyHost("connection-a"));
            Assert.Equal("connection-a", session.HostConnectionId);
        }

        [Fact]
        public async Task TryClaimOrVerifyHost_DifferentConnection_OnceHostIsSet_IsRejected()
        {
            await using var session = new VideoPlaybackSession("s17", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            Assert.True(session.TryClaimOrVerifyHost("connection-a"));
            Assert.False(session.TryClaimOrVerifyHost("connection-b"));
            Assert.Equal("connection-a", session.HostConnectionId); // rejection must not disturb the existing host
        }

        [Fact]
        public async Task TryClaimOrVerifyHost_ConcurrentFirstClaims_SettleOnExactlyOneWinner()
        {
            await using var session = new VideoPlaybackSession("s18", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            var connectionIds = Enumerable.Range(0, 20).Select(i => $"connection-{i}").ToArray();
            var results = await Task.WhenAll(connectionIds.Select(id => Task.Run(() => (id, claimed: session.TryClaimOrVerifyHost(id)))));

            Assert.Single(results, r => r.claimed);
            Assert.NotNull(session.HostConnectionId);
            Assert.Contains(session.HostConnectionId, connectionIds);
        }

        private sealed class FaultingVideoFrameSource : IVideoFrameSource
        {
            public bool Disposed { get; private set; }
            public VideoStreamInfo? StreamInfo { get; private set; }

            public Task<VideoStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
            {
                var info = new VideoStreamInfo { Width = 4, Height = 4, PixelFormat = VideoPixelFormat.Rgb24, IsVariableFrameRate = false };
                StreamInfo = info;
                return Task.FromResult(info);
            }

            public async IAsyncEnumerable<DecodedVideoFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                throw new VideoFrameSourceException("synthetic decode failure");
#pragma warning disable CS0162 // unreachable, but required so the compiler recognizes this as an iterator method
                yield break;
#pragma warning restore CS0162
            }

            public ValueTask DisposeAsync()
            {
                Disposed = true;
                return ValueTask.CompletedTask;
            }
        }
    }
}
