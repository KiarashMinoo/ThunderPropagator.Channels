using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Channels.Demo.VideoPlayer.Configuration;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;
using ThunderPropagator.Channels.Demo.VideoPlayer.Metadata;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Channel
{
    public
#if !DEBUG
        sealed
#endif
        class VideoPlayerChannel : AbstractChannel<VideoPlayerChannelMetadata, VideoPlayerChannelConfiguration>
    {
        // Resolved optionally, not via GetRequiredService — AddVideoPlayerChannel (#238) always
        // registers VideoPlaybackSessionManager, but this channel type can still be constructed directly
        // in a test/host that never called it. Mirrors NotificationsChannel's own constructor, which
        // resolves an unrelated optional dependency the same gracefully-degrading way.
        // OnSubscriptionRemoved below simply no-ops while this is null.
        private readonly VideoPlaybackSessionManager? _sessionManager;

        public VideoPlayerChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _sessionManager = serviceProvider.GetService<VideoPlaybackSessionManager>();
        }

        /// <summary>
        /// Pushes <paramref name="message"/> to every subscriber of this channel instance. Thin wrapper
        /// around the protected base broadcast primitive so pipelines (a separate class hierarchy, not
        /// derived from <see cref="AbstractChannel{TMetadata,TConfiguration}"/>) can invoke it — mirrors
        /// <c>RockPaperScissorsChannel.SendAsync</c>'s own equivalent wrapper.
        /// </summary>
        internal Task BroadcastAsync(VideoPlayerChannelFeederMessage message, CancellationToken cancellationToken = default) =>
            EmitMessageAsync(message, cancellationToken);

        /// <summary>
        /// Synchronous sibling of <see cref="BroadcastAsync"/> — #231's own scope, for
        /// <see cref="OnSubscriptionRemoved"/>, which is itself a synchronous <see langword="void"/> hook
        /// (mirrors <c>ChatChannel.EmitMessage</c>'s own equivalent sync wrapper).
        /// </summary>
        internal void Broadcast(VideoPlayerChannelFeederMessage message) => EmitMessage(message);

        /// <summary>
        /// Detects a viewer disconnecting and, if it was the departing connection's own session's
        /// current host, applies #231's deterministic reassignment and broadcasts the resulting state —
        /// #231's own scope, "On host disconnect, reassign deterministically to the next active eligible
        /// subscriber" and "Broadcast host changes." The actual reassignment logic lives entirely in
        /// <see cref="VideoPlaybackSession.Unsubscribe"/> (already exhaustively tested at that level, see
        /// <c>VideoPlaybackSessionTests</c>) — this override's own job is purely finding the right session
        /// (see <see cref="VideoPlaybackSessionManager.TryFindSessionForConnection"/>'s own remarks for why
        /// that needs a scan rather than this channel knowing its own key) and deciding whether the
        /// resulting host actually changed before broadcasting.
        /// </summary>
        protected override void OnSubscriptionRemoved(Subscription subscription)
        {
            var connectionId = subscription.ConnectionInfo.ConnectionId;

            if (_sessionManager is not null && _sessionManager.TryFindSessionForConnection(connectionId, out var session) && session is not null)
            {
                var previousHost = session.HostConnectionId;
                session.Unsubscribe(connectionId);
                var newHost = session.HostConnectionId;

                if (newHost != previousHost)
                {
                    var snapshot = session.PeekSnapshot();
                    var feederMessage = new VideoPlayerChannelFeederMessage
                    {
                        SessionId = session.SessionId,
                        State = snapshot.State,
                        Epoch = snapshot.Epoch,
                        CurrentFrameNumber = snapshot.FrameNumber,
                        MediaPosition = (long)snapshot.MediaPosition.TotalMicroseconds,
                        SyncTime = (long)snapshot.SyncTime.TotalMicroseconds,
                        ViewerCount = session.ViewerCount
                    };

                    // Host's own setter throws on an empty string (see VideoPlayerChannelFeederMessage.
                    // Host's own ValidateNonEmpty) — when newHost is null (no subscribers left at all),
                    // simply never assign it, the same convention Play/Pause/Seek/Select/React already
                    // use for VideoId/Title when they have no data for a field, leaving it at its own
                    // GetValueOrDefault(string.Empty).
                    if (newHost is not null)
                        feederMessage.Host = newHost;

                    Broadcast(feederMessage);
                }
            }

            base.OnSubscriptionRemoved(subscription);
        }
    }
}
