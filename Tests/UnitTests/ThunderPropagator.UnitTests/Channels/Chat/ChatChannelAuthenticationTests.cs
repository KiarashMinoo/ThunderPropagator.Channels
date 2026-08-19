using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Pipelines;

namespace ThunderPropagator.UnitTests.Channels.Chat
{
    /// <summary>
    /// Issue #106: ChatChannelSendMessageReceiverPipeline used to index LoggedInUsers directly, so
    /// an unauthenticated connection (never logged in, or logged in on a connection that has since
    /// disconnected) triggered an unhandled KeyNotFoundException instead of a controlled Unauthorized
    /// response. ChatChannel.TryGetLoggedInUserId replaces that indexing — these tests cover its
    /// three states (missing, removed, valid session) directly, since the pipeline's own Invoke
    /// method can't be exercised in isolation (ChannelInfo's constructor is internal to a
    /// closed-source assembly, the same limitation noted for Notifications' receive pipelines).
    /// Issue #109 generalized the pipeline-specific unauthorized exception this originally covered
    /// into the shared ChatChannelUnauthorizedException used by every authenticated pipeline; see
    /// ChatChannelPipelineAuthenticationTests for the guard's cross-pipeline enforcement.
    /// </summary>
    public sealed class ChatChannelAuthenticationTests
    {
        private static ChatChannel CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(ChatChannelConfiguration)).Returns(new ChatChannelConfiguration());

            var channel = new ChatChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        private static Subscription CreateSubscription(ChatChannel channel, string connectionId)
        {
            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns(connectionId);

            return new Subscription(connectionInfo, channel, "request-1", "subscription-1", [], new Dictionary<string, ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.ChannelProgramsDescriptor>());
        }

        private static void InvokeOnSubscriptionRemoved(ChatChannel channel, Subscription subscription)
        {
            var method = typeof(ChatChannel).GetMethod("OnSubscriptionRemoved", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException(typeof(ChatChannel).FullName, "OnSubscriptionRemoved");
            method.Invoke(channel, [subscription]);
        }

        [Fact]
        public void TryGetLoggedInUserId_ForAConnectionThatNeverLoggedIn_ReturnsFalse()
        {
            var channel = CreateChannel();

            var found = channel.TryGetLoggedInUserId("never-logged-in", out var userId);

            Assert.False(found);
            Assert.Equal(Guid.Empty, userId);
        }

        [Fact]
        public void TryGetLoggedInUserId_ForALoggedInConnection_ReturnsTrueAndTheUserId()
        {
            var channel = CreateChannel();
            var expectedUserId = Guid.NewGuid();
            channel.LoggedInUsers["connection-1"] = expectedUserId;

            var found = channel.TryGetLoggedInUserId("connection-1", out var userId);

            Assert.True(found);
            Assert.Equal(expectedUserId, userId);
        }

        [Fact]
        public void TryGetLoggedInUserId_ForARemovedSession_ReturnsFalse()
        {
            var channel = CreateChannel();
            channel.LoggedInUsers["connection-1"] = Guid.NewGuid();
            var subscription = CreateSubscription(channel, "connection-1");

            InvokeOnSubscriptionRemoved(channel, subscription);

            var found = channel.TryGetLoggedInUserId("connection-1", out _);
            Assert.False(found);
        }

        [Fact]
        public void ChatChannelUnauthorizedException_IsException()
        {
            var type = typeof(ChatChannelUnauthorizedException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void ChatChannelUnauthorizedException_IsInternal()
        {
            var type = typeof(ChatChannelUnauthorizedException);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void ChatChannelUnauthorizedException_MessageCarriesNoSessionDetails()
        {
            var exception = new ChatChannelUnauthorizedException();

            Assert.DoesNotContain("connection-1", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Guid.Empty.ToString(), exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
