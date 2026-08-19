using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #76: broadcast/group-vs-individual routing used to be inferred from whether UserId (and,
    /// since #74, GroupId) happened to be set, which overloaded nullability with routing semantics
    /// and made invalid combinations (e.g. a "broadcast" that still carries a stale UserId) easy to
    /// construct. NotificationAudience makes the intent explicit, and
    /// NotificationsChannelFeederMessage.ValidateAudienceCombination enforces which of UserId/GroupId
    /// each value requires or forbids. That check runs at the channel's emission boundary against the
    /// caller-authored message only — never against the per-recipient copies the channel's own
    /// fan-out constructs internally, which legitimately combine a non-Individual Audience with a
    /// UserId once routed to a specific recipient (see NotificationsChannelGroupRoutingTests).
    /// </summary>
    public sealed class NotificationsChannelAudienceTests
    {
        public sealed class TestNotificationsChannelConfiguration : AbstractChannelConfiguration;

        private static NotificationsChannel<TestNotificationsChannelConfiguration> CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(TestNotificationsChannelConfiguration))
                .Returns(new TestNotificationsChannelConfiguration { IsEnabled = true });

            var channel = new NotificationsChannel<TestNotificationsChannelConfiguration>(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        [Fact]
        public void Audience_DefaultsToIndividual()
        {
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };

            Assert.Equal(NotificationAudience.Individual, message.Audience);
        }

        [Fact]
        public async Task Individual_WithUserIdSet_Succeeds()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Individual };

            var exception = await Record.ExceptionAsync(() => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Null(exception);
        }

        [Fact]
        public async Task Individual_WithUserIdAndGroupIdBothSet_Succeeds()
        {
            // GroupId is allowed alongside Individual purely for categorization/filtering (see #74's
            // SearchHistoricalNotificationsAsync groupId filter) — it has no routing effect unless
            // Audience is Group.
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { UserId = "user-1", GroupId = "group-a", Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Individual };

            var exception = await Record.ExceptionAsync(() => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Null(exception);
        }

        [Fact]
        public async Task Individual_WithoutUserId_Throws()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Individual };

            var exception = await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Audience), exception.PropertyName);
        }

        [Fact]
        public async Task Group_WithGroupIdSetAndUserIdUnset_Succeeds()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { GroupId = "group-a", Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Group };

            var exception = await Record.ExceptionAsync(() => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Null(exception);
        }

        [Fact]
        public async Task Group_WithoutGroupId_Throws()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Group };

            var exception = await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Audience), exception.PropertyName);
        }

        [Fact]
        public async Task Group_WithUserIdAlsoSet_Throws()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { UserId = "user-1", GroupId = "group-a", Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Group };

            var exception = await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Audience), exception.PropertyName);
        }

        [Fact]
        public async Task Broadcast_WithNeitherUserIdNorGroupIdSet_Succeeds()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Broadcast };

            var exception = await Record.ExceptionAsync(() => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Null(exception);
        }

        [Fact]
        public async Task Broadcast_WithUserIdSet_Throws()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Broadcast };

            var exception = await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Audience), exception.PropertyName);
        }

        [Fact]
        public async Task Broadcast_WithGroupIdSet_Throws()
        {
            // Covers the "does not leak stale recipient identifiers" acceptance criterion: a
            // Broadcast-audience message that still carries a GroupId (e.g. copied from a Group
            // message without clearing it) must be rejected rather than silently routed.
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { GroupId = "group-a", Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Broadcast };

            var exception = await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Audience), exception.PropertyName);
        }

        [Fact]
        public async Task Broadcast_WithBothUserIdAndGroupIdSet_Throws()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { UserId = "user-1", GroupId = "group-a", Id = "notification-1", Subject = "subject", Audience = NotificationAudience.Broadcast };

            var exception = await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Audience), exception.PropertyName);
        }
    }
}
