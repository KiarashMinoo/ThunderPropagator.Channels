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

        protected override void OnSubscriptionRemoved(Subscription subscription)
        {
            LoggedInUsers.TryRemove(subscription.ConnectionInfo.ConnectionId, out _);
            base.OnSubscriptionRemoved(subscription);
        }

        internal void EmitMessage(ChatChannelFeederMessage feederMessage) => base.EmitMessage(feederMessage);
    }
}