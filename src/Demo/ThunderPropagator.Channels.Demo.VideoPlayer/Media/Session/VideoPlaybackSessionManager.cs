using System.Collections.Concurrent;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session
{
    /// <summary>
    /// Owns every active <see cref="VideoPlaybackSession"/>, keyed by <see cref="VideoPlaybackSession.SessionId"/>
    /// — #220's own scope, "Create thread-safe session state keyed by SessionId." Concurrent
    /// <see cref="GetOrCreateSession"/> calls for the same id always resolve to the same single instance
    /// and construct at most one — #220's own AC extended to session creation itself: joining viewers
    /// (which is what repeated <see cref="GetOrCreateSession"/> calls for one id model) never duplicates
    /// anything.
    /// </summary>
    /// <remarks>
    /// <b>Host shutdown:</b> a non-cancelable <c>hostShutdownToken</c> constructor argument (the default) means
    /// this manager is disposed only when its owner explicitly calls <see cref="DisposeAsync"/>. A
    /// cancelable one (e.g. <c>IHostApplicationLifetime.ApplicationStopping</c> in real DI wiring) is
    /// also registered here to dispose this manager — and therefore every session and its media work —
    /// automatically on host shutdown, on top of each session's own generations already linking that same
    /// token (see <see cref="VideoPlaybackSession"/>'s own constructor) — #220's own AC, "Link
    /// cancellation to session removal and host shutdown."
    /// </remarks>
    public sealed class VideoPlaybackSessionManager : IAsyncDisposable
    {
        private readonly ConcurrentDictionary<string, Lazy<VideoPlaybackSession>> _sessions = new();
        private readonly Func<string, VideoPlaybackSession> _sessionFactory;
        private bool _disposed;

        public VideoPlaybackSessionManager(Func<string, VideoPlaybackSession> sessionFactory, CancellationToken hostShutdownToken = default)
        {
            ArgumentNullException.ThrowIfNull(sessionFactory);
            _sessionFactory = sessionFactory;

            if (hostShutdownToken.CanBeCanceled)
                hostShutdownToken.Register(() => _ = DisposeAsync());
        }

        /// <summary>Number of sessions currently tracked (including any whose media work has since faulted or ended, until removed).</summary>
        public int SessionCount => _sessions.Count;

        /// <summary>
        /// Returns the existing session for <paramref name="sessionId"/>, or constructs and registers a
        /// new one via the factory passed to this manager's constructor. Thread-safe: concurrent calls
        /// for the same id never construct more than one session, even though the underlying dictionary
        /// primitive alone would not guarantee that on its own.
        /// </summary>
        public VideoPlaybackSession GetOrCreateSession(string sessionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ObjectDisposedException.ThrowIf(_disposed, this);

            // ConcurrentDictionary.GetOrAdd may invoke a value-factory more than once under a race and
            // simply discard every result but the winning one — harmless for a cheap Lazy<T> wrapper, but
            // not for VideoPlaybackSession's own constructor if it ever grows side effects. Wrapping the
            // real factory in Lazy<T> (ExecutionAndPublication) guarantees the *session* factory itself
            // still runs at most once, regardless of how many throwaway Lazy wrappers a dictionary race
            // creates and discards.
            var lazy = _sessions.GetOrAdd(sessionId, id => new Lazy<VideoPlaybackSession>(() => _sessionFactory(id), LazyThreadSafetyMode.ExecutionAndPublication));
            return lazy.Value;
        }

        /// <summary>Returns the session for <paramref name="sessionId"/> without creating one. <see langword="false"/> if none is registered (or one is still being constructed by a concurrent <see cref="GetOrCreateSession"/> call).</summary>
        public bool TryGetSession(string sessionId, out VideoPlaybackSession? session)
        {
            if (_sessions.TryGetValue(sessionId, out var lazy) && lazy.IsValueCreated)
            {
                session = lazy.Value;
                return true;
            }

            session = null;
            return false;
        }

        /// <summary>Removes and disposes <paramref name="sessionId"/>'s own session — cancels and disposes all its media work. <see langword="false"/> if it was not registered.</summary>
        public async Task<bool> RemoveSessionAsync(string sessionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

            if (!_sessions.TryRemove(sessionId, out var lazy))
                return false;

            await lazy.Value.DisposeAsync().ConfigureAwait(false);
            return true;
        }

        /// <summary>Disposes every currently registered session. Safe to call more than once.</summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            var sessions = _sessions.Values.ToArray();
            _sessions.Clear();

            foreach (var lazy in sessions)
                await lazy.Value.DisposeAsync().ConfigureAwait(false);
        }
    }
}
