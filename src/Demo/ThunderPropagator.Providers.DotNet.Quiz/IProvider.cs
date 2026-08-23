namespace ThunderPropagator.Providers.DotNet.Quiz
{
    /// <summary>
    /// A "provider": a DI-registered service a host application calls on demand to push its own
    /// externally-produced state into <typeparamref name="TChannel"/>, as opposed to
    /// <c>IterativeFeeder</c>'s self-driven polling loop (compare <c>IFeederHandler{TChannel,TMessage}</c>,
    /// which this mirrors in shape). No such contract exists yet anywhere in the ThunderPropagator
    /// packages this repo currently depends on — checked exhaustively (source, every restored package's
    /// XML docs, and a full reflection scan of every assembly reachable from this solution) while
    /// building this ticket. This interface is this repo's own minimal placeholder for it, so a real
    /// framework-level <c>IProvider</c> can converge on (or replace) this later without changing the
    /// call shape a host already depends on.
    /// </summary>
    /// <typeparam name="TChannel">The channel this provider publishes into.</typeparam>
    /// <typeparam name="TMessage">The provider's own public message type — never the channel's internal wire message type directly.</typeparam>
    public interface IProvider<TChannel, in TMessage>
        where TChannel : class
    {
        /// <summary>
        /// Publishes <paramref name="message"/> into the channel this provider was constructed for.
        /// Implementations must check <paramref name="cancellationToken"/> before doing any work, and
        /// must never swallow a validation or channel-level failure — both must propagate to the caller
        /// exactly as thrown.
        /// </summary>
        Task PublishAsync(TMessage message, CancellationToken cancellationToken = default);
    }
}
