namespace ThunderPropagator.Channels.Games.RockPaperScissors.Models
{
    // Issue #288: the persisted replacement for RockPaperScissorsChannel's old node-local
    // _matchedConnectionIds set — a row here means "this connection has already played a resolved
    // match and is no longer eligible to be handed out as an opponent." Persisted so the reservation
    // is visible cluster-wide and durable across a process restart, not just node-local in-process
    // state. There is no removal path — once reserved, a connectionId stays reserved for its whole
    // lifetime, mirroring the original dictionary's own behavior (nothing in this codebase ever
    // called TryRemove on it either); a rematch happens under a fresh connection, not by freeing this
    // one.
    public
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsMatchReservation
    {
        public string ConnectionId { get; } = null!;
        public DateTimeOffset ReservedAt { get; }

        private RockPaperScissorsMatchReservation()
        {
        }

        private RockPaperScissorsMatchReservation(string connectionId, DateTimeOffset reservedAt)
        {
            ConnectionId = connectionId;
            ReservedAt = reservedAt;
        }

        // Public rather than internal like most other Create factories in this codebase (e.g.
        // User.Create) — a reservation carries no business validation to protect (just a connectionId
        // and a timestamp), and the provider implementing TryReserveConnectionAsync (a different
        // assembly from this core one — InMemory/EntityFrameworkCore/MongoDB) needs to construct one
        // as part of its own atomic reserve-or-fail operation.
        public static RockPaperScissorsMatchReservation Create(string connectionId) => new(connectionId, DateTimeOffset.UtcNow);
    }
}
