using System.Collections.Concurrent;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Messages;
using ThunderPropagator.Channels.Chat.Metadata;

namespace ThunderPropagator.Channels.Chat.Channel
{
    public
#if !DEBUG
        sealed
#endif
        class ChatChannel(IServiceProvider serviceProvider) : AbstractChannel<ChatChannelMetadata, ChatChannelConfiguration>(serviceProvider)
    {
        internal ConcurrentDictionary<string, Guid> LoggedInUsers { get; } = new();

        /// <summary>
        /// Resolves the UserId a connection logged in as (see #106), without throwing when it never
        /// logged in or its session was removed (e.g. on disconnect, see
        /// <see cref="OnSubscriptionRemoved"/>) since it did — a protected pipeline's own
        /// authentication check should use this instead of indexing <see cref="LoggedInUsers"/>
        /// directly, which throws <see cref="KeyNotFoundException"/> for exactly that case.
        /// </summary>
        internal bool TryGetLoggedInUserId(string connectionId, out Guid userId) => LoggedInUsers.TryGetValue(connectionId, out userId);

        /// <summary>
        /// Issue #121: atomically removes a connection's session, returning the userId it was logged
        /// in as only when this call is the one that actually removed it — <see cref="ConcurrentDictionary{TKey,TValue}.TryRemove(TKey,out TValue)"/>
        /// is inherently atomic, so a repeated call (or one racing <see cref="OnSubscriptionRemoved"/>
        /// cleaning up the same connection on disconnect) safely returns false instead of throwing or
        /// removing twice. ChatChannelLogoutReceiverPipeline uses the false/true distinction to decide
        /// whether to publish an offline notification at all — a no-op repeat logout publishes
        /// nothing, exactly once for whichever call (explicit logout or disconnect) actually won the
        /// race.
        /// </summary>
        internal bool TryLogOut(string connectionId, out Guid userId) => LoggedInUsers.TryRemove(connectionId, out userId);

        // Issue #121: reuses TryLogOut rather than indexing LoggedInUsers directly, so an explicit
        // Logout call and a disconnect for the same connection racing each other go through the
        // exact same atomic removal — whichever wins gets true (and, for Logout, is the one that
        // publishes the offline notification), the other gets false and does nothing further.
        // Disconnect itself does not publish presence — that would need a scoped UserService to look
        // up contacts, and OnSubscriptionRemoved runs outside any pipeline's request scope; explicit
        // Logout is the only presence-publishing path this ticket adds.
        protected override void OnSubscriptionRemoved(Subscription subscription)
        {
            TryLogOut(subscription.ConnectionInfo.ConnectionId, out _);
            base.OnSubscriptionRemoved(subscription);
        }

        internal void EmitMessage(ChatChannelFeederMessage feederMessage) => base.EmitMessage(feederMessage);
    }
}
