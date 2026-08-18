using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #73: NotificationsChannelFeederMessage.ExpiresAt is only meaningful if the channel
    /// actually enforces it — these tests cover the channel side of that contract: exclusion from
    /// SearchHistoricalNotificationsAsync, SnapshotsToSendAsync (replay-on-subscribe), and missed-
    /// broadcast catch-up, plus the skip-rather-than-throw behavior when a message is already
    /// expired at the moment it's emitted. A TestTimeProvider stands in for TimeProvider.System so
    /// pre-expiry, boundary, and post-expiry cases are deterministic rather than racing the real
    /// clock.
    /// </summary>
    public sealed class NotificationsChannelExpirationTests
    {
        public sealed class TestNotificationsChannelConfiguration : AbstractChannelConfiguration;

        private sealed class TestTimeProvider : TimeProvider
        {
            public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => UtcNow;
        }

        private sealed record CreatedChannel(
            NotificationsChannel<TestNotificationsChannelConfiguration> Channel,
            TestTimeProvider TimeProvider,
            FakeLogCollector LogCollector);

        private static CreatedChannel CreateChannel()
        {
            var timeProvider = new TestTimeProvider();
            var loggerProvider = new FakeLoggerProvider();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(loggerProvider));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(loggerFactory);
            serviceProvider.GetService(typeof(TimeProvider)).Returns(timeProvider);
            serviceProvider.GetService(typeof(TestNotificationsChannelConfiguration))
                .Returns(new TestNotificationsChannelConfiguration { IsEnabled = true });

            var channel = new NotificationsChannel<TestNotificationsChannelConfiguration>(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return new CreatedChannel(channel, timeProvider, loggerProvider.Collector);
        }

        private static void EnableSnapshotting(NotificationsChannel<TestNotificationsChannelConfiguration> channel)
        {
            for (var type = channel.Metadata.GetType(); type is not null; type = type.BaseType)
            {
                var method = type.GetMethod("SetChannelSnapshot", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method is null)
                    continue;

                var arguments = method.GetParameters()
                    .Select(parameter => parameter.Name == "isEnabled" ? true : Type.Missing)
                    .ToArray();

                method.Invoke(channel.Metadata,
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.OptionalParamBinding,
                    null, arguments, null);
                return;
            }

            throw new MissingMethodException(channel.Metadata.GetType().FullName, "SetChannelSnapshot");
        }

        private static Subscription CreateSubscription(NotificationsChannel<TestNotificationsChannelConfiguration> channel, string userId)
        {
            var keyDescriptor = new SubscribingKeyChannelProgramsDescriptor(0, nameof(NotificationsChannelFeederMessage.UserId));
            var subscribedKeys = new[] { new SubscribedKey(keyDescriptor, userId) };
            IReadOnlyDictionary<string, ChannelProgramsDescriptor> subscribedFields = new Dictionary<string, ChannelProgramsDescriptor>();

            return new Subscription(Substitute.For<IConnectionInfo>(), channel, "request-1", "subscription-1", subscribedKeys, subscribedFields);
        }

        private static Task InvokeSendMissedBroadcastsAsync(NotificationsChannel<TestNotificationsChannelConfiguration> channel, string userId)
        {
            var method = typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).GetMethod("SendMissedBroadcastsAsync", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException(typeof(NotificationsChannel<TestNotificationsChannelConfiguration>).FullName, "SendMissedBroadcastsAsync");
            return (Task)method.Invoke(channel, [userId, CancellationToken.None])!;
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_BeforeExpiry_StillReturnsTheMessage()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            var expiresAt = created.TimeProvider.UtcNow.UtcDateTime.AddHours(1);
            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject", ExpiresAt = expiresAt },
                CancellationToken.None);

            var history = await created.Channel.SearchHistoricalNotificationsAsync("user-1");

            Assert.Single(history);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_AtTheExactExpiryInstant_ExcludesTheMessage()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            var expiresAt = created.TimeProvider.UtcNow.UtcDateTime.AddHours(1);
            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject", ExpiresAt = expiresAt },
                CancellationToken.None);

            created.TimeProvider.UtcNow = new DateTimeOffset(expiresAt, TimeSpan.Zero);

            var history = await created.Channel.SearchHistoricalNotificationsAsync("user-1");

            Assert.Empty(history);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_AfterExpiry_ExcludesTheMessage()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            var expiresAt = created.TimeProvider.UtcNow.UtcDateTime.AddHours(1);
            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject", ExpiresAt = expiresAt },
                CancellationToken.None);

            created.TimeProvider.UtcNow = new DateTimeOffset(expiresAt.AddSeconds(1), TimeSpan.Zero);

            var history = await created.Channel.SearchHistoricalNotificationsAsync("user-1");

            Assert.Empty(history);
        }

        [Fact]
        public async Task SearchHistoricalNotificationsAsync_MessageWithNoExpiresAt_NeverExcluded()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject" },
                CancellationToken.None);

            created.TimeProvider.UtcNow = created.TimeProvider.UtcNow.AddYears(10);

            var history = await created.Channel.SearchHistoricalNotificationsAsync("user-1");

            Assert.Single(history);
        }

        [Fact]
        public async Task SnapshotsToSendAsync_ExcludesAnAlreadyExpiredEntry()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            var expiresAt = created.TimeProvider.UtcNow.UtcDateTime.AddHours(1);
            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject", ExpiresAt = expiresAt },
                CancellationToken.None);

            created.TimeProvider.UtcNow = new DateTimeOffset(expiresAt.AddSeconds(1), TimeSpan.Zero);

            var subscription = CreateSubscription(created.Channel, "user-1");
            var snapshotsToSend = await created.Channel.SnapshotsToSendAsync(subscription, CancellationToken.None);

            Assert.Empty(snapshotsToSend);
        }

        [Fact]
        public async Task EmitMessageAsync_AlreadyExpired_DoesNotThrow()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            var alreadyExpired = created.TimeProvider.UtcNow.UtcDateTime.AddSeconds(-1);
            var exception = await Record.ExceptionAsync(() => iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject", ExpiresAt = alreadyExpired },
                CancellationToken.None));

            Assert.Null(exception);
        }

        [Fact]
        public async Task EmitMessageAsync_AlreadyExpired_IsNeverStored()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            var alreadyExpired = created.TimeProvider.UtcNow.UtcDateTime.AddSeconds(-1);
            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject", ExpiresAt = alreadyExpired },
                CancellationToken.None);

            var history = await created.Channel.SearchHistoricalNotificationsAsync("user-1");

            Assert.Empty(history);
        }

        [Fact]
        public async Task EmitMessageAsync_AlreadyExpired_LogsAnInformationalNotice()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            var alreadyExpired = created.TimeProvider.UtcNow.UtcDateTime.AddSeconds(-1);
            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject", ExpiresAt = alreadyExpired },
                CancellationToken.None);

            var notice = Assert.Single(created.LogCollector.GetSnapshot(), record => record.Level == LogLevel.Information);
            Assert.Contains("expired", notice.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EmitMessageAsync_BroadcastAlreadyExpired_ProducesNoRecipientCopies()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            // Register a known broadcast recipient the same way NotificationsChannelBroadcastIsolationTests does.
            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", CastType = CastType.Broadcast, Id = "seed-user-1", Subject = "subject" },
                CancellationToken.None);

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            created.Channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            var alreadyExpired = created.TimeProvider.UtcNow.UtcDateTime.AddSeconds(-1);
            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { Id = "broadcast-1", Subject = "subject", ExpiresAt = alreadyExpired },
                CancellationToken.None);

            Assert.Empty(emitted);
        }

        [Fact]
        public async Task MissedBroadcastCatchUp_ExcludesAnExpiredBroadcast()
        {
            var created = CreateChannel();
            EnableSnapshotting(created.Channel);
            IChannel iChannel = created.Channel;

            // Seed a known recipient the same way NotificationsChannelBroadcastIsolationTests does
            // — fan-out discovery only finds users who already have a stored CastType.Broadcast
            // snapshot entry, so an un-seeded broadcast would fan out to nobody and the resulting
            // "nothing to replay" would pass this test vacuously rather than exercising the filter.
            await iChannel.EmitMessageAsync(
                new NotificationsChannelFeederMessage { UserId = "user-1", CastType = CastType.Broadcast, Id = "seed-user-1", Subject = "subject" },
                CancellationToken.None);

            var expiresAt = created.TimeProvider.UtcNow.UtcDateTime.AddHours(1);
            var broadcast = new NotificationsChannelFeederMessage { Id = "broadcast-1", Subject = "subject", ExpiresAt = expiresAt };
            await iChannel.EmitMessageAsync(broadcast, CancellationToken.None);

            created.TimeProvider.UtcNow = new DateTimeOffset(expiresAt.AddSeconds(1), TimeSpan.Zero);

            var emitted = new List<IReadOnlyDictionary<string, object?>>();
            created.Channel.MessageEmitting += (_, feederMessage) => emitted.Add(feederMessage);

            await InvokeSendMissedBroadcastsAsync(created.Channel, "user-new");

            // The seed broadcast itself (never expired) is legitimately caught up alongside it, so
            // the assertion targets the expired entry specifically rather than requiring an empty
            // catch-up altogether.
            Assert.DoesNotContain(emitted, feederMessage => Equals(feederMessage[nameof(NotificationsChannelFeederMessage.Id)], "broadcast-1"));
        }
    }
}
