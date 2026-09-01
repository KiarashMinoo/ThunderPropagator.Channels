using System.Collections.Concurrent;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.Channels.Games.TicTacToe.InMemory.Context
{
    /// <summary>
    /// Holds the actual in-memory state for the TicTacToe domain (one game record per session) —
    /// register this as a singleton (see <see cref="Extensions.InMemoryTicTacToeExtensions"/>) and let
    /// <see cref="InMemoryTicTacToeContext"/>, which is scoped like every other
    /// <see cref="ITicTacToeContext"/> implementation, wrap it. Mirrors
    /// ThunderPropagator.Channels.Games.RockPaperScissors.InMemory's own InMemoryRockPaperScissorsStore
    /// — see its own doc comment for the full reasoning (a fresh scoped context must not mean fresh,
    /// empty data; <see cref="Reset"/> exists for test isolation between cases sharing one store).
    /// </summary>
    public sealed class InMemoryTicTacToeStore
    {
        private readonly ConcurrentDictionary<string, TicTacToeGameRecord> _games = new();

        /// <summary>Clears every game — for test isolation between cases sharing one store.</summary>
        public void Reset() => _games.Clear();

        internal ConcurrentDictionary<string, TEntity> GetStore<TEntity>() where TEntity : class
        {
            if (typeof(TEntity) == typeof(TicTacToeGameRecord)) return (ConcurrentDictionary<string, TEntity>)(object)_games;

            throw new NotSupportedException($"No store for {typeof(TEntity).Name}.");
        }

        internal static string GetId<TEntity>(TEntity entity) where TEntity : class => entity switch
        {
            TicTacToeGameRecord game => game.SessionId,
            _ => throw new NotSupportedException($"No id accessor for {typeof(TEntity).Name}.")
        };

        internal static string ToKey<TPk>(TPk id)
            => id as string ?? throw new NotSupportedException($"Unsupported id type '{typeof(TPk).Name}'.");

        internal TEntity Add<TEntity>(TEntity entity) where TEntity : class
        {
            GetStore<TEntity>()[GetId(entity)] = entity;
            return entity;
        }
    }
}
