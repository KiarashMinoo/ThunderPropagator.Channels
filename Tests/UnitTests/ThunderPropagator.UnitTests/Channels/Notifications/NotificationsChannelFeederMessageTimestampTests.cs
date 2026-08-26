using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #64: Date and Time used to fall back to DateTime.UtcNow on every read when unset, so
    /// re-reading the same message could observe different timestamps, and Date/Time could
    /// disagree with each other. The constructor now captures one UTC instant and initializes both
    /// from it, so a message's timestamp is fixed at construction time.
    /// </summary>
    public sealed class NotificationsChannelFeederMessageTimestampTests
    {
        private sealed class TestTimeProvider : TimeProvider
        {
            public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => UtcNow;
        }

        [Fact]
        public void RepeatedReads_OfDateAndTime_AreStableDespiteTheClockAdvancing()
        {
            // Issue #78: proves the same regression the delay-based version of this test proved
            // (a buggy Date/Time getter falling back to a live clock read on every call would
            // observe the clock having moved), but deterministically — advancing a fake clock
            // instead of waiting on the real one.
            var timeProvider = new TestTimeProvider();
            var message = new NotificationsChannelFeederMessage(timeProvider);

            var firstDate = message.Date;
            var firstTime = message.Time;

            timeProvider.UtcNow = timeProvider.UtcNow.AddMilliseconds(50);

            Assert.Equal(firstDate, message.Date);
            Assert.Equal(firstTime, message.Time);
        }

        [Fact]
        public void DateAndTime_OriginateFromTheSameCapturedInstant()
        {
            var message = new NotificationsChannelFeederMessage();

            Assert.Equal(message.Date.TimeOfDay, message.Time);
        }

        [Fact]
        public void Date_IsCapturedAtConstructionTime()
        {
            var before = DateTime.UtcNow;
            var message = new NotificationsChannelFeederMessage();
            var after = DateTime.UtcNow;

            Assert.InRange(message.Date, before, after);
        }

        [Fact]
        public void Copying_APreviouslyConstructedMessage_DoesNotReplaceItsTimestamp()
        {
            var source = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };
            var originalDate = source.Date;
            var originalTime = source.Time;

            var copy = new NotificationsChannelFeederMessage(source);

            Assert.Equal(originalDate, copy.Date);
            Assert.Equal(originalTime, copy.Time);
        }

        [Fact]
        public void DictionaryConstruction_WithAnExplicitTimestamp_PreservesIt()
        {
            var explicitDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            IDictionary<string, object?> raw = new Dictionary<string, object?>
            {
                [nameof(NotificationsChannelFeederMessage.Date)] = explicitDate,
                [nameof(NotificationsChannelFeederMessage.Time)] = explicitDate.TimeOfDay,
                [nameof(NotificationsChannelFeederMessage.Id)] = "notification-1",
                [nameof(NotificationsChannelFeederMessage.Subject)] = "subject"
            };

            var message = new NotificationsChannelFeederMessage(raw);

            Assert.Equal(explicitDate, message.Date);
            Assert.Equal(explicitDate.TimeOfDay, message.Time);
        }
    }
}
