using System.Collections.Concurrent;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;

namespace ThunderPropagator.Channels.Chat
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

        protected override void OnSubscriptionRemoved(Subscription subscription)
        {
            LoggedInUsers.TryRemove(subscription.ConnectionInfo.ConnectionId, out _);
            base.OnSubscriptionRemoved(subscription);
        }

        internal void EmitMessage(ChatChannelFeederMessage feederMessage) => base.EmitMessage(feederMessage);
    }
}
