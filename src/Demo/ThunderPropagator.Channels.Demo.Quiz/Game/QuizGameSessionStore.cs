using System.Collections.Concurrent;

namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// The registry of every currently known <see cref="QuizGameSession"/>, keyed by
    /// <see cref="QuizGameSession.GameId"/>. One instance is meant to be shared for the lifetime of the
    /// host process (a future ticket's DI registration decides that — see #187's own scope note below);
    /// this type only owns looking sessions up, creating them on first use, and releasing them once
    /// abandoned. Each session's own lock guards its internal state (see <see cref="QuizGameSession"/>'s
    /// own remarks), so this store never needs to lock across sessions — <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// alone is enough to keep two different GameIds from ever affecting each other (#187's own AC).
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizGameSessionStore
    {
        private readonly ConcurrentDictionary<string, QuizGameSession> _sessionsByGameId = new(StringComparer.Ordinal);

        /// <summary>The number of sessions currently tracked, including ones no player is connected to yet.</summary>
        public int SessionCount => _sessionsByGameId.Count;

        /// <summary>
        /// The session for <paramref name="gameId"/>, creating a brand-new, empty one on first use.
        /// Safe under concurrent calls for the same <paramref name="gameId"/> — exactly one
        /// <see cref="QuizGameSession"/> instance is ever observed for a given GameId, even if the
        /// underlying factory races (the standard <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey,Func{TKey,TValue})"/>
        /// guarantee); a QuizGameSession's constructor has no side effects, so a discarded race loser
        /// is harmless.
        /// </summary>
        public QuizGameSession GetOrCreateSession(string gameId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameId);

            return _sessionsByGameId.GetOrAdd(gameId, static id => new QuizGameSession(id));
        }

        /// <summary>The session for <paramref name="gameId"/>, or null if none has been created yet.</summary>
        public QuizGameSession? TryGetSession(string gameId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameId);

            return _sessionsByGameId.GetValueOrDefault(gameId);
        }

        /// <summary>
        /// Removes <paramref name="gameId"/>'s session if it exists and is currently
        /// <see cref="QuizGameSession.IsAbandoned"/> (#187's own AC on releasing abandoned games).
        /// Returns whether a session was actually removed — false when the GameId is unknown, or when
        /// it still has at least one connected player. A session that reaches abandonment and then
        /// reconnects before this is called simply survives, since the check and the removal both read
        /// the current state at call time rather than latching an earlier observation.
        /// </summary>
        public bool RemoveIfAbandoned(string gameId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameId);

            if (!_sessionsByGameId.TryGetValue(gameId, out var session) || !session.IsAbandoned)
                return false;

            // Re-checked against the exact instance just inspected (ConcurrentDictionary's own
            // TryRemove(key, comparisonValue) overload) so a session that got recreated for the same
            // GameId between the check above and this call is never removed out from under its new
            // occupants.
            return _sessionsByGameId.TryRemove(new KeyValuePair<string, QuizGameSession>(gameId, session));
        }
    }
}
