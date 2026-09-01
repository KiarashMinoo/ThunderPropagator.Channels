using System.Collections.Concurrent;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.InMemory.Context
{
    /// <summary>
    /// Holds the actual in-memory state for the RockPaperScissors domain (match reservations, session
    /// records) — register this as a singleton (see <see cref="Extensions.InMemoryRockPaperScissorsExtensions"/>)
    /// and let <see cref="InMemoryRockPaperScissorsContext"/>, which is scoped like every other
    /// <see cref="IRockPaperScissorsContext"/> implementation, wrap it. Mirrors
    /// ThunderPropagator.Channels.Chat.InMemory's own InMemoryChatStore — see its own doc comment for
    /// the full reasoning (a fresh scoped context must not mean fresh, empty data; <see cref="Reset"/>
    /// exists for test isolation between cases sharing one store).
    ///
    /// Unlike InMemoryChatStore, no cross-entity uniqueness check needs a shared lock here —
    /// <see cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/> alone already gives
    /// <see cref="TryReserveConnection"/> the exact atomicity <see cref="IRockPaperScissorsContext.TryReserveConnectionAsync"/>
    /// requires, and session records have no uniqueness constraint to enforce at all (each carries its
    /// own freshly-generated SessionId).
    /// </summary>
    public sealed class InMemoryRockPaperScissorsStore
    {
        private readonly ConcurrentDictionary<string, RockPaperScissorsMatchReservation> _reservations = new();
        private readonly ConcurrentDictionary<string, RockPaperScissorsGameSessionRecord> _sessions = new();

        /// <summary>Clears every collection — for test isolation between cases sharing one store.</summary>
        public void Reset()
        {
            _reservations.Clear();
            _sessions.Clear();
        }

        internal ConcurrentDictionary<string, TEntity> GetStore<TEntity>() where TEntity : class
        {
            if (typeof(TEntity) == typeof(RockPaperScissorsMatchReservation)) return (ConcurrentDictionary<string, TEntity>)(object)_reservations;
            if (typeof(TEntity) == typeof(RockPaperScissorsGameSessionRecord)) return (ConcurrentDictionary<string, TEntity>)(object)_sessions;

            throw new NotSupportedException($"No store for {typeof(TEntity).Name}.");
        }

        internal static string GetId<TEntity>(TEntity entity) where TEntity : class => entity switch
        {
            RockPaperScissorsMatchReservation reservation => reservation.ConnectionId,
            RockPaperScissorsGameSessionRecord session => session.SessionId,
            _ => throw new NotSupportedException($"No id accessor for {typeof(TEntity).Name}.")
        };

        internal static string ToKey<TPk>(TPk id)
            => id as string ?? throw new NotSupportedException($"Unsupported id type '{typeof(TPk).Name}'.");

        internal TEntity Add<TEntity>(TEntity entity) where TEntity : class
        {
            GetStore<TEntity>()[GetId(entity)] = entity;
            return entity;
        }

        /// <summary>
        /// Atomically claims <paramref name="connectionId"/>, returning false if it was already
        /// claimed — the same guarantee <see cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/> gave
        /// the original in-memory-only implementation this store replaces.
        /// </summary>
        internal bool TryReserveConnection(string connectionId)
            => _reservations.TryAdd(connectionId, RockPaperScissorsMatchReservation.Create(connectionId));
    }
}
