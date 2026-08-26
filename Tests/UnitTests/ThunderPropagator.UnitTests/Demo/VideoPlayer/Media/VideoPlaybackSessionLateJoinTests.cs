using ThunderPropagator.Channels.Demo.VideoPlayer;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Issue #223's own ACs: a late join starts at the current live position (never frame 0), never
    /// restarts/duplicates decoding, the snapshot and its bootstrap frame always belong to the same
    /// epoch, racing joins never produce a duplicate or visibly-rewound frame, and a paused join
    /// receives the paused frame.
    /// </summary>
    public sealed class VideoPlaybackSessionLateJoinTests
    {
        private static readonly VideoSource TestSource = new() { Location = "synthetic://test" };

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

                await Task.Delay(2);
            }
        }

        [Fact]
        public async Task Join_WithNothingPublishedYet_ReturnsASnapshotWithoutABootstrapFrame()
        {
            await using var session = new VideoPlaybackSession("s1", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

            var snapshot = session.Join("viewer");

            Assert.False(snapshot.HasBootstrapFrame);
            Assert.Equal(PlayState.Loading, snapshot.State);
            Assert.Equal(0, snapshot.FrameNumber);
            Assert.Equal(TimeSpan.Zero, snapshot.MediaPosition);
            Assert.Equal(TimeSpan.Zero, snapshot.SyncTime);
            Assert.False(session.TryDequeue("viewer", out _));
        }

        [Fact]
        public async Task Join_AfterPlaybackHasAdvanced_BootstrapsTheCurrentFrame_NeverFrameZero()
        {
            // A slow-ish rate so the stream is still mid-flight (not yet Ended) when this joins.
            var options = new VideoPlaybackSessionOptions { PlaybackRate = 3.0, PollInterval = TimeSpan.FromMilliseconds(2) };
            await using var session = new VideoPlaybackSession("s2", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughEncode);

            // A probe subscriber, not the test's own late joiner, deterministically detects "more than one
            // frame has already published" regardless of system load — a fixed sleep here would be a
            // flake waiting to happen (see feedback_async_test_pitfalls memory: leave real margin, don't
            // guess a wall-clock delay that "should" be enough).
            session.Subscribe("probe");
            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() =>
            {
                var sawAnother = false;
                while (session.TryDequeue("probe", out var packet))
                    sawAnother = packet!.FrameNumber > 0;

                return sawAnother;
            }, TimeSpan.FromSeconds(20));

            var snapshot = session.Join("lateViewer");

            Assert.True(snapshot.HasBootstrapFrame);
            Assert.True(snapshot.FrameNumber > 0, "a late join must start at the current position, not frame 0");

            Assert.True(session.TryDequeue("lateViewer", out var firstReceived));
            Assert.Equal(snapshot.FrameNumber, firstReceived!.FrameNumber);
            Assert.Equal(snapshot.Epoch, firstReceived.Epoch);
        }

        [Fact]
        public async Task Join_SnapshotEpoch_AlwaysMatchesItsOwnBootstrapFramesEpoch()
        {
            await using var session = new VideoPlaybackSession("s3", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            await session.SelectAsync(TestSource);
            await Task.Delay(10);

            var snapshot = session.Join("viewer");

            if (snapshot.HasBootstrapFrame)
            {
                Assert.True(session.TryDequeue("viewer", out var bootstrap));
                Assert.Equal(snapshot.Epoch, bootstrap!.Epoch);
            }
        }

        [Fact]
        public async Task Join_NeverReopensTheSourceOrDisturbsAnAlreadySubscribedViewer()
        {
            var openCount = 0;

            IVideoFrameSource Factory()
            {
                Interlocked.Increment(ref openCount);
                return new SyntheticVideoFrameSource();
            }

            // Real-time (not FastOptions()-style accelerated) so this whole synthetic stream (~380ms)
            // can't finish naturally before this test gets around to joining/draining a second time —
            // see feedback_async_test_pitfalls memory: leave real margin, don't guess a rate/delay that
            // "should" be enough under load.
            var options = new VideoPlaybackSessionOptions { PlaybackRate = 1.0, PollInterval = TimeSpan.FromMilliseconds(2) };
            await using var session = new VideoPlaybackSession("s4", Factory, new SystemMonotonicClock(), options, PassthroughEncode);
            session.Subscribe("early");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.TryDequeue("early", out _), TimeSpan.FromSeconds(20));

            // Drain whatever "early" already has so we can observe it keeps advancing, uninterrupted, after the join below.
            long? lastEarlySeen = null;
            while (session.TryDequeue("early", out var packet))
                lastEarlySeen = packet!.FrameNumber;

            session.Join("late");

            // Accumulated across polls (not just within one) — WaitUntilAsync's own condition check
            // dequeues as a side effect, so a plain "TryDequeue once more after waiting" would lose
            // whichever packet the successful poll itself already consumed.
            var earlyContinued = false;
            await WaitUntilAsync(() =>
            {
                while (session.TryDequeue("early", out var packet))
                {
                    if (lastEarlySeen is { } last)
                        Assert.True(packet!.FrameNumber > last, "the already-subscribed viewer's own sequence must never reset or go backward because someone else joined");

                    lastEarlySeen = packet!.FrameNumber;
                    earlyContinued = true;
                }

                return earlyContinued;
            }, TimeSpan.FromSeconds(20));

            Assert.True(earlyContinued, "playback should have kept advancing for the early viewer while this test ran");
            Assert.Equal(1, openCount);
        }

        [Fact]
        public async Task Join_WhilePaused_ReceivesThePausedFrame_AndNothingMoreUntilResume()
        {
            // Real-time (not accelerated) — see the sibling test's own remarks on why: PauseAsync needs
            // to run before this synthetic stream (~380ms) finishes naturally, and a faster rate leaves
            // too little real-world margin for that under load.
            var options = new VideoPlaybackSessionOptions { PlaybackRate = 1.0, PollInterval = TimeSpan.FromMilliseconds(2) };
            await using var session = new VideoPlaybackSession("s5", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughEncode);
            session.Subscribe("early");

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => session.TryDequeue("early", out _), TimeSpan.FromSeconds(20));
            while (session.TryDequeue("early", out _)) { }

            await session.PauseAsync();

            var snapshot = session.Join("lateViewer");

            Assert.Equal(PlayState.Paused, snapshot.State);
            Assert.True(snapshot.HasBootstrapFrame);
            Assert.True(session.TryDequeue("lateViewer", out _));

            await Task.Delay(30); // several poll intervals while still paused
            Assert.False(session.TryDequeue("lateViewer", out _), "no further frames should arrive for a joiner while paused");

            await session.ResumeAsync();
            await WaitUntilAsync(() => session.TryDequeue("lateViewer", out _), TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task Join_RepeatedlyDuringActivePublishing_NeverProducesADuplicateOrRewoundFrame()
        {
            var options = new VideoPlaybackSessionOptions { PlaybackRate = 5.0, PollInterval = TimeSpan.FromMilliseconds(1) };
            await using var session = new VideoPlaybackSession("s6", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughEncode);

            await session.SelectAsync(TestSource);

            for (var i = 0; i < 25; i++)
            {
                var viewerId = $"joiner{i}";
                var snapshot = session.Join(viewerId);

                var received = new List<long>();
                if (snapshot.HasBootstrapFrame)
                {
                    Assert.True(session.TryDequeue(viewerId, out var bootstrap));
                    received.Add(bootstrap!.FrameNumber);
                }

                await Task.Delay(1);
                while (session.TryDequeue(viewerId, out var packet))
                    received.Add(packet!.FrameNumber);

                for (var j = 1; j < received.Count; j++)
                    Assert.True(received[j] > received[j - 1], $"viewer {viewerId} received frame numbers out of order or duplicated: [{string.Join(", ", received)}]");
            }
        }

        [Fact]
        public async Task Join_ConcurrentlyWithSeek_NeverThrows_AndProducesASelfConsistentSnapshot()
        {
            var options = new VideoPlaybackSessionOptions { PlaybackRate = 50_000, PollInterval = TimeSpan.FromMilliseconds(1) };

            await using var session = new VideoPlaybackSession("s7", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), options, PassthroughEncode);
            await session.SelectAsync(TestSource);

            for (var i = 0; i < 10; i++)
            {
                var seekTask = session.SeekAsync(TimeSpan.Zero);
                LateJoinSnapshot? snapshot = null;
                var joinTask = Task.Run(() => snapshot = session.Join($"racer{i}"));

                await Task.WhenAll(seekTask, joinTask);

                Assert.NotNull(snapshot);
                if (snapshot!.HasBootstrapFrame)
                {
                    Assert.True(session.TryDequeue($"racer{i}", out var bootstrap));
                    Assert.Equal(snapshot.Epoch, bootstrap!.Epoch);
                    Assert.Equal(snapshot.FrameNumber, bootstrap.FrameNumber);
                }
            }
        }
    }
}
