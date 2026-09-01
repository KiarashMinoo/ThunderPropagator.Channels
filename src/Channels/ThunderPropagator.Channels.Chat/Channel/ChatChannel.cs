using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Channels.Chat.Configuration;
using ThunderPropagator.Channels.Chat.Messages;
using ThunderPropagator.Channels.Chat.Metadata;
using ThunderPropagator.Channels.Chat.Models.Sessions;

namespace ThunderPropagator.Channels.Chat.Channel
{
    public
#if !DEBUG
        sealed
#endif
        partial class ChatChannel : AbstractChannel<ChatChannelMetadata, ChatChannelConfiguration>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly CancellationToken _cancellationToken;

        // Issue #46: ChatUserSessionService is Scoped (like GroupService/MessageService/UserService),
        // so it can't be injected directly into this Singleton channel — a fresh scope is created
        // per disconnect instead, mirroring ChatContextInitializationHostedService's own reasoning
        // for the same constraint.
        public ChatChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            _cancellationToken = serviceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        }

        // Issue #121: disconnect cleanup so a connection that drops without an explicit Logout
        // (crash, network loss, forced close) doesn't stay "online" forever. Issue #46: now goes
        // through the persisted ChatUserSessionService instead of the old in-memory LoggedInUsers
        // dictionary, so cleanup is visible cluster-wide, not just to whichever node held the
        // connection. Fire-and-forget for the same reason NotificationsChannel.OnSubscriptionAdded
        // is: this hook is a synchronous, non-awaitable override with no async equivalent available;
        // failures are logged rather than thrown. Disconnect itself still does not publish a presence
        // notification — that remains explicit Logout's job alone (see
        // ChatChannelLogoutReceiverPipeline), consistent with #121's original reasoning.
        protected override void OnSubscriptionRemoved(Subscription subscription)
        {
            _ = CleanUpSessionOnDisconnectAsync(subscription.ConnectionInfo.ConnectionId, _cancellationToken);
            base.OnSubscriptionRemoved(subscription);
        }

        private async Task CleanUpSessionOnDisconnectAsync(string connectionId, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sessionService = scope.ServiceProvider.GetRequiredService<ChatUserSessionService>();
                await sessionService.LogOutAsync(connectionId, cancellationToken);
            }
            catch (Exception exception)
            {
                Log.DisconnectSessionCleanupFailed(Logger, exception, connectionId, Metadata.ChannelName);
            }
        }

        /// <summary>
        /// Issue #46: replaces the old node-local <c>TryGetLoggedInUserId</c> dictionary lookup —
        /// used by <see cref="AuthenticatedChatChannelReceiverPipeline"/>'s shared Invoke, which
        /// would otherwise need <see cref="ChatUserSessionService"/>, itself Scoped, threaded through
        /// every one of its ~20 derived pipelines' constructors just for this one check. Exposing the
        /// lookup here instead — a fresh scope created per call, same as
        /// <see cref="CleanUpSessionOnDisconnectAsync"/> — keeps that base class dependency-free.
        /// This does mean every authenticated request now costs a DB round trip for identity instead
        /// of an in-memory lookup, but every pipeline already pays at least one DB round trip for its
        /// own domain logic (GroupService/MessageService/UserService), so this isn't a new order of
        /// latency, just a cluster-safety trade a single node-local dictionary couldn't make.
        /// </summary>
        internal async Task<Guid?> TryGetLoggedInUserIdAsync(string connectionId, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<ChatUserSessionService>();
            return await sessionService.GetUserIdAsync(connectionId, cancellationToken);
        }

        internal void EmitMessage(ChatChannelFeederMessage feederMessage) => base.EmitMessage(feederMessage);

        // Issue #39: LoggerMessage-generated methods for this channel's own log call sites. EventId
        // 1104 continues this project's own block (1101-1103 are ChatContextInitializationHostedService's).
        private static partial class Log
        {
            /// <summary>Logs that cleaning up a disconnected connection's chat session failed.</summary>
            [LoggerMessage(EventId = 1104, Level = LogLevel.Error, Message = "Failed to clean up chat session for disconnected connection {ConnectionId} on channel {ChannelName}.")]
            public static partial void DisconnectSessionCleanupFailed(ILogger logger, Exception exception, string connectionId, string channelName);
        }
    }
}
