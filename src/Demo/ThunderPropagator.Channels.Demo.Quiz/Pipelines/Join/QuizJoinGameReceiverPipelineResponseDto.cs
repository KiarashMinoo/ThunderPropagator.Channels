using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Collections;

namespace ThunderPropagator.Channels.Demo.Quiz.Pipelines.Join
{
    public
#if !DEBUG
        sealed
#endif
        class QuizJoinGameReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        /// <summary>The subscription this join established — the game's current state then arrives as a single unicast snapshot through this subscription, via the channel's usual snapshot-replay-on-subscribe.</summary>
        public required Subscription Subscription { get; init; }

        /// <summary>Whether this call reconnected an existing, previously-disconnected player rather than adding a brand-new one — see #187's own documented reconnect policy.</summary>
        public required bool IsReconnect { get; init; }

        /// <summary>Whether the joining player is this game's host (the player whose join created the session).</summary>
        public required bool IsHost { get; init; }

        /// <summary>The player's name as actually recorded — after normalization, which may differ from what was requested.</summary>
        public required string PlayerName { get; init; }
    }
}
