using System.Collections.Concurrent;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Channels.Games.RockPaperScissors.Configuration;
using ThunderPropagator.Channels.Games.RockPaperScissors.Messages;
using ThunderPropagator.Channels.Games.RockPaperScissors.Metadata;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.Channel
{
    public
#if !DEBUG
        sealed
#endif
        class RockPaperScissorsChannel : AbstractChannel<RockPaperScissorsChannelMetadata, RockPaperScissorsChannelConfiguration>
    {
        // Issue #12's own scope, "keep a session for the game": every resolved match, keyed by its own
        // server-generated SessionId — see RockPaperScissorsGameSession's own remarks on why this stays
        // server-side only.
        private readonly ConcurrentDictionary<string, RockPaperScissorsGameSession> _sessions = new();

        // ConnectionIds already consumed by a resolved session (as either player) — PeekRandomPlayer
        // excludes these so a subscriber who has already played is never handed out as a second player's
        // own opponent again. A narrow, accepted race remains: two concurrent PlayWithHuman calls could
        // both pick the same not-yet-recorded opponent before either call's own RecordSession runs: this
        // module has no distributed/per-match lock, matching this codebase's own established demo-quality
        // bar elsewhere (e.g. PortfolioDemoChannel's own search-then-mutate snapshot calls aren't atomic
        // across the whole operation either).
        private readonly ConcurrentDictionary<string, byte> _matchedConnectionIds = new();

        public RockPaperScissorsChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>The current subscription for <paramref name="connectionId"/>, or <see langword="null"/> if it is not (or no longer) subscribed to this channel.</summary>
        internal Subscription? FindSubscription(string connectionId) =>
            Subscriptions.Subscriptions.FirstOrDefault(subscription => subscription.ConnectionInfo.ConnectionId == connectionId);

        /// <summary>
        /// A random currently-subscribed player available to be matched against, excluding
        /// <paramref name="excludeConnectionId"/> (the player requesting a match — issue #12's own fix:
        /// the original implementation could match a player against themselves) and every connection
        /// already recorded in a resolved <see cref="RockPaperScissorsGameSession"/> (issue #12's own
        /// fix: the original implementation could hand out an already-played subscriber as a second
        /// player's own opponent, indefinitely). <see langword="null"/> if nobody is currently eligible.
        /// </summary>
        internal Subscription? PeekRandomPlayer(string? excludeConnectionId)
        {
            var candidates = Subscriptions.Subscriptions
                .Where(subscription => subscription.ConnectionInfo.ConnectionId != excludeConnectionId
                    && !_matchedConnectionIds.ContainsKey(subscription.ConnectionInfo.ConnectionId))
                .ToArray();

            return candidates.Length == 0 ? null : candidates[Random.Shared.Next(candidates.Length)];
        }

        /// <summary>Records a resolved match — issue #12's own scope, "keep a session for the game" — and marks both real (non-computer) players as no longer eligible for future matchmaking.</summary>
        internal RockPaperScissorsGameSession RecordSession(Player firstPlayer, Player secondPlayer)
        {
            var session = new RockPaperScissorsGameSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                FirstPlayer = firstPlayer,
                SecondPlayer = secondPlayer,
                PlayedAt = DateTimeOffset.UtcNow
            };

            _sessions.TryAdd(session.SessionId, session);

            if (firstPlayer.Subscription is not null)
                _matchedConnectionIds.TryAdd(firstPlayer.Subscription.ConnectionInfo.ConnectionId, 0);

            if (secondPlayer.Subscription is not null)
                _matchedConnectionIds.TryAdd(secondPlayer.Subscription.ConnectionInfo.ConnectionId, 0);

            return session;
        }

        /// <summary>Every resolved match recorded so far — a point-in-time snapshot, safe to enumerate while more sessions are being recorded concurrently.</summary>
        internal IReadOnlyCollection<RockPaperScissorsGameSession> GetSessions() => _sessions.Values.ToArray();

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
