using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Channels.Games.RockPaperScissors.Configuration;
using ThunderPropagator.Channels.Games.RockPaperScissors.Messages;
using ThunderPropagator.Channels.Games.RockPaperScissors.Metadata;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.Channel
{
    public
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsChannel : AbstractChannel<RockPaperScissorsChannelMetadata, RockPaperScissorsChannelConfiguration>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // Issue #288: RockPaperScissorsMatchmakingService is Scoped (like Chat's
        // ChatUserSessionService — see #46), so it can't be injected directly into this Singleton
        // channel — a fresh scope is created per call instead, mirroring ChatChannel's own reasoning
        // for the same constraint.
        public RockPaperScissorsChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        private Task<T> UseMatchmakingServiceAsync<T>(Func<RockPaperScissorsMatchmakingService, Task<T>> action)
        {
            using var scope = _scopeFactory.CreateScope();
            return action(scope.ServiceProvider.GetRequiredService<RockPaperScissorsMatchmakingService>());
        }

        /// <summary>The current subscription for <paramref name="connectionId"/>, or <see langword="null"/> if it is not (or no longer) subscribed to this channel.</summary>
        internal Subscription? FindSubscription(string connectionId) =>
            Subscriptions.Subscriptions.FirstOrDefault(subscription => subscription.ConnectionInfo.ConnectionId == connectionId);

        /// <summary>
        /// A random currently-subscribed player available to be matched against, excluding
        /// <paramref name="excludeConnectionId"/> (the player requesting a match — issue #12's own fix:
        /// the original implementation could match a player against themselves) and every connection
        /// already reserved by a resolved match (issue #12's own fix: the original implementation could
        /// hand out an already-played subscriber as a second player's own opponent, indefinitely).
        /// <see langword="null"/> if nobody is currently eligible.
        /// </summary>
        /// <remarks>
        /// Issue #21: selection and reservation happen as one atomic step, same as before — the
        /// returned player's ConnectionId is already reserved (see
        /// <see cref="RockPaperScissorsMatchmakingService.TryReserveConnectionAsync"/>) by the time this
        /// returns, so two simultaneous callers can never both walk away with the same opponent.
        /// Candidates are shuffled and tried in order, falling through to the next one if a concurrent
        /// caller (on this node or another) wins the reservation for a given candidate first.
        ///
        /// Issue #288: the candidate pool itself — <see cref="AbstractChannel{TMetadata,TConfiguration}.Subscriptions"/>
        /// — remains node-local; the framework has no cluster-wide view of live subscriptions to draw
        /// from (see #46's own findings on this same limitation for presence). This fix makes the
        /// reservation itself cluster-safe (durable, visible everywhere), which is as far as this
        /// mechanism can go without the framework exposing cross-node subscription visibility — a
        /// human opponent must currently be subscribed to this same node to be found at all.
        /// </remarks>
        internal Task<Subscription?> PeekRandomPlayerAsync(string? excludeConnectionId, CancellationToken cancellationToken = default)
            => UseMatchmakingServiceAsync(async matchmaking =>
            {
                var candidates = Subscriptions.Subscriptions
                    .Where(subscription => subscription.ConnectionInfo.ConnectionId != excludeConnectionId)
                    .OrderBy(_ => Random.Shared.Next())
                    .ToArray();

                foreach (var candidate in candidates)
                {
                    if (await matchmaking.TryReserveConnectionAsync(candidate.ConnectionInfo.ConnectionId, cancellationToken))
                        return candidate;
                }

                return null;
            });

        /// <summary>Records a resolved match — issue #12's own scope, "keep a session for the game" — and marks both real (non-computer) players as no longer eligible for future matchmaking.</summary>
        internal async Task RecordSessionAsync(Player firstPlayer, Player secondPlayer, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var matchmaking = scope.ServiceProvider.GetRequiredService<RockPaperScissorsMatchmakingService>();

            await matchmaking.RecordSessionAsync(firstPlayer, secondPlayer, cancellationToken);

            // Reaffirms whichever of the two wasn't already reserved during PeekRandomPlayerAsync's
            // own selection step (the requesting player, who is never itself a "candidate") —
            // idempotent/harmless for the one that was (see TryReserveConnectionAsync's own
            // contract: already-reserved is a no-op false, not a throw).
            if (firstPlayer.Subscription is not null)
                await matchmaking.TryReserveConnectionAsync(firstPlayer.Subscription.ConnectionInfo.ConnectionId, cancellationToken);

            if (secondPlayer.Subscription is not null)
                await matchmaking.TryReserveConnectionAsync(secondPlayer.Subscription.ConnectionInfo.ConnectionId, cancellationToken);
        }

        /// <summary>Every resolved match recorded so far, cluster-wide.</summary>
        internal Task<IReadOnlyCollection<RockPaperScissorsGameSessionRecord>> GetSessionsAsync(CancellationToken cancellationToken = default)
            => UseMatchmakingServiceAsync(matchmaking => matchmaking.GetSessionsAsync(cancellationToken));

        /// <summary>
        /// Issue #12's own fix for the previous no-op <c>SendAsync</c> stub (which called a
        /// <c>base.SendAsync</c> overload that no longer exists on <see cref="AbstractChannel{TMetadata,TConfiguration}"/>
        /// in this package version, and so was commented out and never actually delivered anything):
        /// delivers <paramref name="message"/> to whichever single subscriber's own subscribed keys
        /// (<see cref="RockPaperScissorsChannelFeederMessage.PlayerName"/>/<see cref="RockPaperScissorsChannelFeederMessage.Opponent"/>/
        /// <see cref="RockPaperScissorsChannelFeederMessage.Move"/> — see
        /// <see cref="RockPaperScissorsChannelMetadata"/>'s own <c>ChannelProgramsDescriptors</c>) match
        /// exactly, mirroring <c>TicTacToeChannel.GameOnBoardChanged</c>'s own already-working use of the
        /// same key-routed <c>EmitMessage</c> overload elsewhere in this codebase.
        /// </summary>
        internal void PushResult(RockPaperScissorsChannelFeederMessage message) => EmitMessage(message);
    }
}
