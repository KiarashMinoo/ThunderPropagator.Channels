using System.Runtime.CompilerServices;
using ThunderPropagator.Channels.Demo.VideoPlayer;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;
using ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Session
{
    /// <summary>
    /// Issue #232's own ACs: at most one mutation changes playback state at a time, old-epoch tasks
    /// cannot publish after a successful seek/select/removal (even if cancellation is observed late),
    /// concurrent commands have deterministic outcomes, no deadlocks occur during cancellation/disposal,
    /// and stress tests cover conflicting Play/Pause/Seek/Select and disconnect operations.
    /// </summary>
    public sealed class VideoPlaybackSessionConcurrencyTests
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

        // #232's own AC, "Old-epoch tasks cannot publish after a successful seek/select/removal, even if
        // cancellation is observed late." Rather than trying to force the exact nanosecond-scale race
        // inside RunPublishLoopAsync's own single cancellation check, this repeats many concurrent
        // Select/Seek pairs against one session while continuously draining every packet either track
        // ever produces, asserting the property that guard is actually meant to protect: once a given
        // epoch has been observed as the session's own current Epoch, no packet claiming an OLDER epoch
        // is ever seen again afterward. Repeating many times raises the odds of genuinely hitting the
        // race this test exists to catch, rather than relying on a single lucky (or unlucky) interleaving.
        [Fact]
        public async Task ConcurrentSeeks_NeverProduceAPacketOlderThanTheEpochAlreadyObserved()
        {
            await using var session = new VideoPlaybackSession("cc1", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);
            session.Subscribe("viewer");

            await session.SelectAsync(TestSource);

            var highestEpochSeen = 0;
            (VideoFramePacket Packet, int HighestEpochSeenAtTheTime)? violation = null;

            using var cts = new CancellationTokenSource();
            var drainTask = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    while (session.TryDequeue("viewer", out var packet))
                    {
                        if (packet!.Epoch < highestEpochSeen && violation is null)
                            violation = (packet, highestEpochSeen);

                        if (packet.Epoch > highestEpochSeen)
                            highestEpochSeen = packet.Epoch;
                    }

                    await Task.Delay(1).ConfigureAwait(false);
                }
            });

            for (var i = 0; i < 15; i++)
            {
                var seekTask = session.SeekAsync(TimeSpan.Zero);
                var selectTask = Task.Run(() => session.SelectAsync(TestSource));
                await Task.WhenAll(seekTask, selectTask);
            }

            await Task.Delay(50); // let the drain loop catch up on whatever the last round published
            cts.Cancel();
            await drainTask;

            Assert.Null(violation);
        }

        // #232's own AC, "Concurrent commands have deterministic documented outcomes" — proves the
        // "last Phase-1-order wins" property SwitchGenerationAsync's own three-phase restructuring is
        // designed to preserve: firing many concurrent Select/Seek calls must always leave the session in
        // one single, self-consistent state, never a torn mix of two attempts' own data.
        [Fact]
        public async Task ConcurrentSelectAndSeek_SettleOnOneSelfConsistentGeneration()
        {
            await using var session = new VideoPlaybackSession("cc2", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            // SeekAsync requires CurrentSource to already be set — establish it synchronously first so
            // every concurrent SeekAsync call below is legitimately valid. Racing a Seek against the
            // very first-ever Select (nothing selected yet) is its own, separate, already-documented
            // InvalidOperationException case, not what this test exists to probe: this test is about
            // determinism among competing switches once a source is already selected, not about that
            // precondition.
            await session.SelectAsync(TestSource);

            var tasks = Enumerable.Range(0, 10)
                .Select(i => i % 2 == 0
                    ? Task.Run(() => session.SelectAsync(TestSource))
                    : Task.Run(() => session.SeekAsync(TimeSpan.FromMilliseconds(i))))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.Same(TestSource, session.CurrentSource);
            Assert.True(session.Epoch > 0);

            var snapshot = session.PeekSnapshot();
            Assert.Equal(session.Epoch, snapshot.Epoch);
        }

        // #232's own AC, "Avoid holding locks across arbitrary user/network callbacks" and "No deadlocks
        // occur during cancellation/disposal" — the concrete, direct test that SwitchGenerationAsync's own
        // three-phase restructuring actually achieves what it's for: a SelectAsync call stuck opening its
        // source must not prevent other, unrelated operations against the same session from completing
        // promptly.
        [Fact]
        public async Task SelectAsync_StuckOpeningASource_DoesNotBlockConcurrentPauseOrJoinOrIsHost()
        {
            var gatedSource = new GatedOpenVideoFrameSource();
            await using var session = new VideoPlaybackSession("cc3", () => gatedSource, new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            var selectTask = session.SelectAsync(TestSource);

            // Give SelectAsync's own Phase 1 (fast, lock-held) a moment to actually run and release the
            // lock before proceeding — otherwise the assertions below could race ahead of it by sheer luck
            // rather than genuinely proving anything about the unlocked Phase 2 this test targets.
            await WaitUntilAsync(() => session.State == PlayState.Loading, TimeSpan.FromSeconds(5));
            Assert.False(selectTask.IsCompleted, "the source's own OpenAsync must still be gated/blocked at this point for this test to mean anything");

            var joinCompleted = Task.Run(() => session.Join("viewer"));
            var isHostCompleted = Task.Run(() => session.IsHost("nobody"));

            await Task.WhenAll(joinCompleted, isHostCompleted).WaitAsync(TimeSpan.FromSeconds(5));

            Assert.True(joinCompleted.IsCompletedSuccessfully, "Join must not be blocked by a concurrent SelectAsync still opening its source");
            Assert.True(isHostCompleted.IsCompletedSuccessfully, "IsHost must not be blocked by a concurrent SelectAsync still opening its source");

            gatedSource.ReleaseOpen();
            await selectTask;
        }

        // Same shape as the test above but for EndAsync specifically — #232's own AC calls out "no
        // deadlocks... during... disposal" by name, and EndAsync's own StopGenerationAsync call now runs
        // outside _lifecycleLock (mirroring DisposeAsync's own already-correct shape) for exactly this
        // reason.
        [Fact]
        public async Task EndAsync_WhileAGenerationIsSlowToStop_DoesNotBlockConcurrentPeekSnapshot()
        {
            var neverRespondingSource = new NeverCancelsReadFramesVideoFrameSource();
            await using var session = new VideoPlaybackSession("cc4", () => neverRespondingSource, new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            await session.SelectAsync(TestSource);
            await WaitUntilAsync(() => neverRespondingSource.ReadFramesAsyncStarted, TimeSpan.FromSeconds(5));

            var endTask = session.EndAsync();

            // EndAsync's own StopGenerationAsync call is now unlocked, so this must complete promptly
            // regardless of whether endTask itself has finished yet (the underlying decode loop may still
            // be shutting down in the background — that's fine, and exactly the point).
            var peekTask = Task.Run(() => session.PeekSnapshot());
            await peekTask.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.True(peekTask.IsCompletedSuccessfully, "PeekSnapshot must not be blocked by a concurrent EndAsync whose own generation is slow to stop");

            neverRespondingSource.AllowCompletion();
            await endTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        // #232's own AC, "Stress tests cover conflicting Play/Pause/Seek/Select and disconnect operations."
        // Fires a genuinely mixed batch of concurrent calls and asserts only invariants that must always
        // hold regardless of exact interleaving — not any specific outcome, since many different orderings
        // are all individually valid.
        [Fact]
        public async Task MixedConcurrentCommandsAndDisconnects_NeverCorruptSessionInvariants()
        {
            await using var session = new VideoPlaybackSession("cc5", () => new SyntheticVideoFrameSource(), new SystemMonotonicClock(), FastOptions(), PassthroughEncode);

            var viewers = Enumerable.Range(0, 6).Select(i => $"viewer{i}").ToArray();
            foreach (var viewer in viewers)
                session.Join(viewer);

            await session.SelectAsync(TestSource);
            var epochBefore = session.Epoch;

            var tasks = new List<Task>
            {
                Task.Run(() => session.PauseAsync()),
                Task.Run(() => session.ResumeAsync()),
                Task.Run(() => session.SeekAsync(TimeSpan.FromMilliseconds(5))),
                Task.Run(() => session.PauseAsync()),
                Task.Run(() => session.SeekAsync(TimeSpan.FromMilliseconds(10))),
                Task.Run(() => session.ResumeAsync())
            };

            // Unsubscribe half the viewers concurrently too, including whichever is currently host.
            foreach (var viewer in viewers.Take(3))
                tasks.Add(Task.Run(() => session.Unsubscribe(viewer)));

            var results = await Task.WhenAll(tasks.Select(async t =>
            {
                try { await t; return (Exception?)null; }
                catch (Exception ex) { return ex; }
            }));

            // InvalidOperationException is an expected, documented outcome here (e.g. a Pause/Resume/Seek
            // racing an EndAsync/a not-yet-selected window) — anything else escaping is a real bug.
            Assert.All(results, ex => Assert.True(ex is null or InvalidOperationException, $"unexpected exception: {ex}"));

            Assert.True(session.Epoch >= epochBefore, "Epoch must never decrease");

            // Only the first 3 viewers were ever unsubscribed above, so the other 3 are always still
            // subscribed regardless of interleaving — meaning the host, whichever of the original 6 it
            // started as, must always have a valid holder to land on and can never legitimately end up
            // null here.
            var remainingViewers = viewers.Skip(3).ToArray();
            var host = session.HostConnectionId;
            Assert.NotNull(host);
            Assert.Contains(host, remainingViewers);

            // The session must still be fully usable afterward.
            await session.SelectAsync(TestSource);
            Assert.Same(TestSource, session.CurrentSource);
        }

        /// <summary>An <see cref="IVideoFrameSource"/> whose <see cref="OpenAsync"/> blocks until <see cref="ReleaseOpen"/> is called (or the caller's own token cancels it) — for proving a slow/stuck open doesn't hold <c>_lifecycleLock</c>.</summary>
        private sealed class GatedOpenVideoFrameSource : IVideoFrameSource
        {
            private readonly TaskCompletionSource _openGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool Disposed { get; private set; }
            public VideoStreamInfo? StreamInfo { get; private set; }

            public void ReleaseOpen() => _openGate.TrySetResult();

            public async Task<VideoStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
            {
                await using (cancellationToken.Register(static state => ((TaskCompletionSource)state!).TrySetCanceled(), _openGate).ConfigureAwait(false))
                    await _openGate.Task.ConfigureAwait(false);

                var info = new VideoStreamInfo { Width = 4, Height = 4, PixelFormat = VideoPixelFormat.Rgb24, IsVariableFrameRate = false, Duration = TimeSpan.FromSeconds(1) };
                StreamInfo = info;
                return info;
            }

            public async IAsyncEnumerable<DecodedVideoFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield break;
            }

            public ValueTask DisposeAsync()
            {
                Disposed = true;
                _openGate.TrySetCanceled();
                return ValueTask.CompletedTask;
            }
        }

        /// <summary>
        /// An <see cref="IVideoFrameSource"/> whose <see cref="ReadFramesAsync"/> opens instantly but then
        /// blocks indefinitely (deliberately never observing cancellation on its own) until
        /// <see cref="AllowCompletion"/> is called — simulates a decode task that doesn't respond to
        /// cancellation promptly, for proving <see cref="VideoPlaybackSession.EndAsync"/>'s own
        /// <c>StopGenerationAsync</c> call no longer holds <c>_lifecycleLock</c> while waiting for one.
        /// </summary>
        private sealed class NeverCancelsReadFramesVideoFrameSource : IVideoFrameSource
        {
            private readonly TaskCompletionSource _completionGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private volatile bool _readFramesAsyncStarted;

            public bool Disposed { get; private set; }
            public VideoStreamInfo? StreamInfo { get; private set; }
            public bool ReadFramesAsyncStarted => _readFramesAsyncStarted;

            public void AllowCompletion() => _completionGate.TrySetResult();

            public Task<VideoStreamInfo> OpenAsync(VideoSource source, CancellationToken cancellationToken = default)
            {
                var info = new VideoStreamInfo { Width = 4, Height = 4, PixelFormat = VideoPixelFormat.Rgb24, IsVariableFrameRate = false, Duration = TimeSpan.FromSeconds(1) };
                StreamInfo = info;
                return Task.FromResult(info);
            }

            public async IAsyncEnumerable<DecodedVideoFrame> ReadFramesAsync(TimeSpan startPosition, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                _readFramesAsyncStarted = true;
                // Deliberately does not register/observe cancellationToken at all — this loop only ever
                // ends once the test itself calls AllowCompletion(), simulating a decode source that is
                // slow (or, in the limit, unresponsive) to cooperative cancellation.
                await _completionGate.Task.ConfigureAwait(false);
                yield break;
            }

            public ValueTask DisposeAsync()
            {
                Disposed = true;
                return ValueTask.CompletedTask;
            }
        }
    }
}
