using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #62: proves BatchSize, DeduplicationWindow, TimeToLive, and MaxDeliveryAttempts are
    /// genuinely actionable, not just stored values, by exercising them through
    /// SampleNotificationsFeederProcessor — a minimal stand-in for the feeder implementations
    /// package consumers write against this configuration.
    /// </summary>
    public sealed class NotificationsFeederConfigurationRuntimeBehaviorTests
    {
        public sealed class TestNotificationsFeederConfiguration : NotificationsFeederConfiguration;

        private static void SetDate(NotificationsChannelFeederMessage message, DateTime date)
            => typeof(NotificationsChannelFeederMessage).GetProperty(nameof(NotificationsChannelFeederMessage.Date))!.GetSetMethod(true)!.Invoke(message, [date]);

        [Fact]
        public void Batching_GroupsMessagesIntoChunksOfAtMostBatchSize()
        {
            var configuration = new TestNotificationsFeederConfiguration { BatchSize = 2 };
            var processor = new SampleNotificationsFeederProcessor(configuration);
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var messages = Enumerable.Range(1, 5)
                .Select(i => new NotificationsChannelFeederMessage { UserId = "user-1", Id = $"notification-{i}" })
                .ToArray();

            var batches = processor.Batch(messages, now);

            Assert.Equal([2, 2, 1], batches.Select(batch => batch.Count));
        }

        [Fact]
        public void Batching_WithDefaultBatchSize_EmitsOneMessagePerBatch()
        {
            var configuration = new TestNotificationsFeederConfiguration();
            var processor = new SampleNotificationsFeederProcessor(configuration);
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var messages = Enumerable.Range(1, 3)
                .Select(i => new NotificationsChannelFeederMessage { UserId = "user-1", Id = $"notification-{i}" })
                .ToArray();

            var batches = processor.Batch(messages, now);

            Assert.Equal([1, 1, 1], batches.Select(batch => batch.Count));
        }

        [Fact]
        public void Deduplication_SuppressesARepeatOfTheSameNotificationWithinTheWindow()
        {
            var configuration = new TestNotificationsFeederConfiguration { DeduplicationWindow = TimeSpan.FromMinutes(5) };
            var processor = new SampleNotificationsFeederProcessor(configuration);
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var original = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1" };
            var duplicate = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1" };

            var batches = processor.Batch([original, duplicate], now);

            var delivered = Assert.Single(batches);
            Assert.Single(delivered);
        }

        [Fact]
        public void Deduplication_DoesNotSuppressADifferentNotificationForTheSameUser()
        {
            var configuration = new TestNotificationsFeederConfiguration { DeduplicationWindow = TimeSpan.FromMinutes(5) };
            var processor = new SampleNotificationsFeederProcessor(configuration);
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var first = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1" };
            var second = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-2" };

            var batches = processor.Batch([first, second], now);

            Assert.Equal(2, batches.SelectMany(batch => batch).Count());
        }

        [Fact]
        public void Deduplication_WithDefaultWindow_NeverSuppressesAnything()
        {
            var configuration = new TestNotificationsFeederConfiguration();
            var processor = new SampleNotificationsFeederProcessor(configuration);
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var original = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1" };
            var duplicate = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1" };

            var batches = processor.Batch([original, duplicate], now);

            Assert.Equal(2, batches.SelectMany(batch => batch).Count());
        }

        [Fact]
        public void Expiration_DropsMessagesOlderThanTimeToLive()
        {
            var configuration = new TestNotificationsFeederConfiguration { TimeToLive = TimeSpan.FromHours(1) };
            var processor = new SampleNotificationsFeederProcessor(configuration);
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var fresh = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "fresh" };
            SetDate(fresh, now.AddMinutes(-30));

            var stale = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "stale" };
            SetDate(stale, now.AddHours(-2));

            var batches = processor.Batch([fresh, stale], now);

            var delivered = Assert.Single(batches);
            var message = Assert.Single(delivered);
            Assert.Equal("fresh", message.Id);
        }

        [Fact]
        public void Expiration_WithNoTimeToLive_NeverDropsMessages()
        {
            var configuration = new TestNotificationsFeederConfiguration();
            var processor = new SampleNotificationsFeederProcessor(configuration);
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var veryOld = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "very-old" };
            SetDate(veryOld, now.AddYears(-1));

            var batches = processor.Batch([veryOld], now);

            var delivered = Assert.Single(batches);
            Assert.Single(delivered);
        }

        [Fact]
        public void RetryLimits_SucceedsIfDeliverySucceedsWithinMaxDeliveryAttempts()
        {
            var configuration = new TestNotificationsFeederConfiguration { MaxDeliveryAttempts = 3 };
            var processor = new SampleNotificationsFeederProcessor(configuration);

            var delivered = processor.TryDeliver(attempt => attempt == 3);

            Assert.True(delivered);
        }

        [Fact]
        public void RetryLimits_GivesUpAfterMaxDeliveryAttemptsAreExhausted()
        {
            var configuration = new TestNotificationsFeederConfiguration { MaxDeliveryAttempts = 2 };
            var processor = new SampleNotificationsFeederProcessor(configuration);
            var attemptCount = 0;

            var delivered = processor.TryDeliver(_ =>
            {
                attemptCount++;
                return false;
            });

            Assert.False(delivered);
            Assert.Equal(2, attemptCount);
        }

        [Fact]
        public void RetryLimits_WithDefaultMaxDeliveryAttempts_MakesOnlyOneAttempt()
        {
            var configuration = new TestNotificationsFeederConfiguration();
            var processor = new SampleNotificationsFeederProcessor(configuration);
            var attemptCount = 0;

            processor.TryDeliver(_ =>
            {
                attemptCount++;
                return false;
            });

            Assert.Equal(1, attemptCount);
        }
    }
}
