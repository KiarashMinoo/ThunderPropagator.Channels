using ThunderPropagator.Channels.Demo.Quiz.Channel;
namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// A "provider": a capability a host application calls on demand to push its own
    /// externally-produced state into a channel, as opposed to <c>IterativeFeeder</c>'s self-driven
    /// polling loop (compare <c>IFeederHandler{TChannel,TMessage}</c>, which this mirrors in shape). No
    /// such contract exists yet anywhere in the ThunderPropagator packages this repo currently depends
    /// on — checked exhaustively (repo source, every restored package's XML docs, and a full reflection
    /// scan of every assembly reachable from this solution) while building this ticket. This interface
    /// is this repo's own minimal placeholder for it, so a real framework-level <c>IProvider</c> can
    /// converge on (or replace) this later without changing the call shape a host already depends on.
    /// Implemented directly by <see cref="QuizChannel"/> itself (#194) rather than a separate wrapper
    /// type or package: this solution is organized around channels, and Quiz is one of its demo
    /// channels, so the capability lives there.
    /// </summary>
    /// <typeparam name="TMessage">The provider's own public message type — never a channel's internal wire message type directly.</typeparam>
    public interface IProvider<in TMessage>
    {
        /// <summary>
        /// Publishes <paramref name="message"/> into the channel implementing this interface.
        /// Implementations must check <paramref name="cancellationToken"/> before doing any work, and
        /// must never swallow a validation or channel-level failure — both must propagate to the caller
        /// exactly as thrown.
        /// </summary>
        Task PublishAsync(TMessage message, CancellationToken cancellationToken = default);
    }
}
