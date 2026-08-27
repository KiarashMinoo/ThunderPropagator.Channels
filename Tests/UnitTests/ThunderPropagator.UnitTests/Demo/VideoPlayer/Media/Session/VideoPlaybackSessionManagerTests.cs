using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;
using ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Video;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer.Media.Session
{
    /// <summary>
    /// Issue #220's own ACs for the keyed session collection: concurrent lifecycle operations for the
    /// same <see cref="VideoPlaybackSession.SessionId"/> never construct more than one session (no
    /// duplicate loops), and removing a session or shutting down the host disposes its media work.
    /// </summary>
    public sealed class VideoPlaybackSessionManagerTests
    {
        private static VideoPlaybackSession CreateSession(string sessionId) =>
            new(sessionId, () => new SyntheticVideoFrameSource(), new SystemMonotonicClock());

        [Fact]
        public void Constructor_WithNullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new VideoPlaybackSessionManager(null!));
        }

        [Fact]
        public async Task GetOrCreateSession_CalledTwiceForTheSameId_ReturnsTheSameInstance_AndFactoryRunsOnce()
        {
            var factoryCallCount = 0;
            var manager = new VideoPlaybackSessionManager(id =>
            {
                Interlocked.Increment(ref factoryCallCount);
                return CreateSession(id);
            });

            var first = manager.GetOrCreateSession("session-1");
            var second = manager.GetOrCreateSession("session-1");

            Assert.Same(first, second);
            Assert.Equal(1, factoryCallCount);

            await manager.DisposeAsync();
        }

        [Fact]
        public async Task GetOrCreateSession_CalledConcurrentlyForTheSameId_ConstructsExactlyOneSession()
        {
            var factoryCallCount = 0;
            var manager = new VideoPlaybackSessionManager(id =>
            {
                Interlocked.Increment(ref factoryCallCount);
                return CreateSession(id);
            });

            // Deliberately no rendezvous barrier here: blocking 50 pool threads on one would starve the
            // thread pool (it injects new threads only slowly under sustained blocking demand), which
            // would stall unrelated concurrently-running tests for tens of seconds. Plain concurrent
            // Task.Run calls already race enough to exercise GetOrCreateSession's own thread-safety.
            var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(() => manager.GetOrCreateSession("shared-session"))));

            Assert.Equal(1, factoryCallCount);
            Assert.All(results, session => Assert.Same(results[0], session));

            await manager.DisposeAsync();
        }

        [Fact]
        public async Task RemoveSessionAsync_DisposesTheSession_AndRemovesItFromTheManager()
        {
            IVideoFrameSource? openedSource = null;
            await using var manager = new VideoPlaybackSessionManager(id => new VideoPlaybackSession(id, () =>
            {
                openedSource = new SyntheticVideoFrameSource();
                return openedSource;
            }, new SystemMonotonicClock()));

            var session = manager.GetOrCreateSession("to-remove");
            await session.SelectAsync(new VideoSource { Location = "synthetic://test" });

            var removed = await manager.RemoveSessionAsync("to-remove");

            Assert.True(removed);
            Assert.False(manager.TryGetSession("to-remove", out _));
            Assert.Equal(0, manager.SessionCount);
        }

        // #236's own scope, "Tests verify no decoder/resource leak after session removal" — the existing
        // RemoveSessionAsync test above only proved the manager forgot the session; this proves the
        // underlying IVideoFrameSource itself was actually disposed on the normal (non-faulted) path,
        // which nothing previously checked.
        [Fact]
        public async Task RemoveSessionAsync_DisposesTheUnderlyingVideoFrameSource_NotJustTheManagerEntry()
        {
            var source = new SyntheticVideoFrameSource();
            await using var manager = new VideoPlaybackSessionManager(id => new VideoPlaybackSession(id, () => source, new SystemMonotonicClock()));

            var session = manager.GetOrCreateSession("to-remove");
            await session.SelectAsync(new VideoSource { Location = "synthetic://test" });

            await manager.RemoveSessionAsync("to-remove");

            Assert.True(source.Disposed);
        }

        // #236's own scope: a single removal proving disposal isn't enough to rule out a slow leak
        // (e.g. a source held onto somewhere and only released "eventually") — this repeats the
        // create-then-remove cycle and asserts every single source across every cycle was disposed, and
        // that the manager itself never accumulates entries across cycles.
        [Fact]
        public async Task RepeatedCreateAndRemoveCycles_DisposeEverySourceExactlyOnce_WithNoAccumulatedSessions()
        {
            const int cycles = 25;
            var openedSources = new List<SyntheticVideoFrameSource>();

            await using var manager = new VideoPlaybackSessionManager(id =>
            {
                var source = new SyntheticVideoFrameSource();
                openedSources.Add(source);
                return new VideoPlaybackSession(id, () => source, new SystemMonotonicClock());
            });

            for (var i = 0; i < cycles; i++)
            {
                var session = manager.GetOrCreateSession($"session-{i}");
                await session.SelectAsync(new VideoSource { Location = "synthetic://test" });
                await manager.RemoveSessionAsync($"session-{i}");
            }

            Assert.Equal(0, manager.SessionCount);
            Assert.Equal(cycles, openedSources.Count);
            Assert.All(openedSources, s => Assert.True(s.Disposed));
        }

        [Fact]
        public async Task RemoveSessionAsync_ForAnUnknownId_ReturnsFalse()
        {
            await using var manager = new VideoPlaybackSessionManager(CreateSession);

            Assert.False(await manager.RemoveSessionAsync("never-created"));
        }

        [Fact]
        public async Task DisposeAsync_DisposesEveryRegisteredSession()
        {
            var manager = new VideoPlaybackSessionManager(CreateSession);
            var sessionA = manager.GetOrCreateSession("a");
            var sessionB = manager.GetOrCreateSession("b");

            await manager.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => sessionA.Subscribe("v"));
            Assert.Throws<ObjectDisposedException>(() => sessionB.Subscribe("v"));
            Assert.Throws<ObjectDisposedException>(() => manager.GetOrCreateSession("c"));
        }

        [Fact]
        public async Task DisposeAsync_IsSafeToCallMoreThanOnce()
        {
            var manager = new VideoPlaybackSessionManager(CreateSession);
            manager.GetOrCreateSession("a");

            await manager.DisposeAsync();
            var exception = await Record.ExceptionAsync(async () => await manager.DisposeAsync());

            Assert.Null(exception);
        }

        [Fact]
        public async Task HostShutdownToken_WhenCancelled_DisposesTheManagerAndItsSessions()
        {
            using var hostShutdown = new CancellationTokenSource();
            var manager = new VideoPlaybackSessionManager(CreateSession, hostShutdown.Token);
            var session = manager.GetOrCreateSession("a");

            hostShutdown.Cancel();

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (true)
            {
                try
                {
                    session.Subscribe("probe");
                    session.Unsubscribe("probe");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Host shutdown never disposed the session.");

                await Task.Delay(5);
            }
        }
    }
}
