using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #73: NotificationsChannelFeederMessage gained an optional ExpiresAt field so a
    /// notification can carry its own per-message expiration rather than relying solely on a
    /// feeder's own TimeToLive default. These tests cover the property itself — default value and
    /// copy-construction propagation — separately from NotificationsChannelExpirationTests, which
    /// covers how the channel actually enforces it.
    /// </summary>
    public sealed class NotificationsChannelFeederMessageExpirationTests
    {
        [Fact]
        public void ExpiresAt_DefaultsToNull()
        {
            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };

            Assert.Null(message.ExpiresAt);
        }

        [Fact]
        public void ExpiresAt_CanBeSetViaObjectInitializer()
        {
            var expiresAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var message = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", ExpiresAt = expiresAt };

            Assert.Equal(expiresAt, message.ExpiresAt);
        }

        [Fact]
        public void CopyConstructor_PropagatesExpiresAtToTheCopy()
        {
            var expiresAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var source = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject", ExpiresAt = expiresAt };

            var copy = new NotificationsChannelFeederMessage(source) { UserId = "user-1" };

            Assert.Equal(expiresAt, copy.ExpiresAt);
        }

        [Fact]
        public void CopyConstructor_WithNoExpiresAtSet_PropagatesNull()
        {
            var source = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };

            var copy = new NotificationsChannelFeederMessage(source) { UserId = "user-1" };

            Assert.Null(copy.ExpiresAt);
        }
    }
}
