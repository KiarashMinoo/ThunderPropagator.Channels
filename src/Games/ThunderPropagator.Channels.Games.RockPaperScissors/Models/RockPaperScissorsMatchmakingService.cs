namespace ThunderPropagator.Channels.Games.RockPaperScissors.Models
{
    // Issue #288: replaces RockPaperScissorsChannel's old node-local _sessions/_matchedConnectionIds
    // dictionaries — see RockPaperScissorsMatchReservation/RockPaperScissorsGameSessionRecord's own
    // doc comments.
    internal
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsMatchmakingService(IRockPaperScissorsContext context)
    {
        /// <summary>
        /// Atomically claims <paramref name="connectionId"/> for matchmaking, returning false if it
        /// was already claimed — the persisted equivalent of the old
        /// ConcurrentDictionary.TryAdd(connectionId, 0) reservation gate.
        /// </summary>
        public Task<bool> TryReserveConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
            => context.TryReserveConnectionAsync(connectionId, cancellationToken);

        /// <summary>Records a resolved match — issue #12's own scope, "keep a session for the game."</summary>
        public Task RecordSessionAsync(Player firstPlayer, Player secondPlayer, CancellationToken cancellationToken = default)
            => context.CreateAsync(RockPaperScissorsGameSessionRecord.Create(firstPlayer, secondPlayer), cancellationToken);

        /// <summary>Every resolved match recorded so far, cluster-wide.</summary>
        public Task<IReadOnlyCollection<RockPaperScissorsGameSessionRecord>> GetSessionsAsync(CancellationToken cancellationToken = default)
            => context.GetAllAsync<RockPaperScissorsGameSessionRecord>(cancellationToken);
    }
}
