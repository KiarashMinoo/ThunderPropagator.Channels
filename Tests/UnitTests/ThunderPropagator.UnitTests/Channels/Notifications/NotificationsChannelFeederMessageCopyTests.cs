using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    public sealed class NotificationsChannelFeederMessageCopyTests
    {
        private static NotificationsChannelFeederMessage CreateSource()
        {
            var source = new NotificationsChannelFeederMessage
            {
                UserId = "user-1",
                Id = "notification-1",
                Seen = 1
            };
            source.CastType = CastType.Broadcast;
            source.IsDeleted = true;
            source.CorrelationId = "correlation-1";
            source.Envelope.HashKey = 12345;

            return source;
        }

        [Fact]
        public void Copy_ContainsEverySourceValue()
        {
            var source = CreateSource();

            var copy = new NotificationsChannelFeederMessage(source);

            // Date/Time are covered separately in Copy_PreservesTimestampsRatherThanRegeneratingThem
            // with an explicitly-set value — left at their lazy "now" default here, reading them
            // twice a few CPU cycles apart could occasionally disagree by a tick and make this
            // specific assertion flaky.
            Assert.Equal(source.UserId, copy.UserId);
            Assert.Equal(source.Id, copy.Id);
            Assert.Equal(source.Seen, copy.Seen);
            Assert.Equal(source.Origin, copy.Origin);
            Assert.Equal(source.Type, copy.Type);
            Assert.Equal(source.Priority, copy.Priority);
            Assert.Equal(source.Icon, copy.Icon);
            Assert.Equal(source.Subject, copy.Subject);
            Assert.Equal(source.Body, copy.Body);
            Assert.Equal(source.EllipsisBody, copy.EllipsisBody);
            Assert.Equal(source.Metadata, copy.Metadata);
            Assert.Equal(source.CastType, copy.CastType);
            Assert.Equal(source.IsDeleted, copy.IsDeleted);
            Assert.Equal(source.CorrelationId, copy.CorrelationId);
            Assert.Equal(source.Envelope.HashKey, copy.Envelope.HashKey);
        }

        [Fact]
        public void Copy_PreservesTimestampsRatherThanRegeneratingThem()
        {
            var source = CreateSource();
            // Date/Time default to "now" the first time they're read if never explicitly set, so
            // force a fixed value to prove the copy doesn't regenerate a fresh timestamp of its own.
            var fixedTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            typeof(NotificationsChannelFeederMessage).GetProperty(nameof(NotificationsChannelFeederMessage.Date))!.GetSetMethod(true)!.Invoke(source, [fixedTime]);
            typeof(NotificationsChannelFeederMessage).GetProperty(nameof(NotificationsChannelFeederMessage.Time))!.GetSetMethod(true)!.Invoke(source, [fixedTime.TimeOfDay]);

            var copy = new NotificationsChannelFeederMessage(source);

            Assert.Equal(fixedTime, copy.Date);
            Assert.Equal(fixedTime.TimeOfDay, copy.Time);
        }

        [Fact]
        public void MutatingTheCopy_DoesNotAffectTheSource()
        {
            var source = CreateSource();
            var copy = new NotificationsChannelFeederMessage(source);

            copy.UserId = "user-2";
            copy.Seen = 99;
            copy.ResetHashKey();

            Assert.Equal("user-1", source.UserId);
            Assert.Equal(1, source.Seen);
            Assert.Equal(12345, source.Envelope.HashKey);
        }

        [Fact]
        public void MutatingOneCopy_DoesNotAffectAnotherCopyOfTheSameSource()
        {
            var source = CreateSource();
            var copyForUserA = new NotificationsChannelFeederMessage(source);
            var copyForUserB = new NotificationsChannelFeederMessage(source);

            copyForUserA.UserId = "user-a";
            copyForUserA.ResetHashKey();

            copyForUserB.UserId = "user-b";

            Assert.Equal("user-a", copyForUserA.UserId);
            Assert.Null(copyForUserA.Envelope.HashKey);
            Assert.Equal("user-b", copyForUserB.UserId);
            Assert.Equal(12345, copyForUserB.Envelope.HashKey);
            Assert.Equal("user-1", source.UserId);
        }

        [Fact]
        public void ResetHashKey_ClearsTheEnvelopeHashKeyUsedByEmission()
        {
            var message = CreateSource();
            Assert.Equal(12345, message.Envelope.HashKey);

            var result = message.ResetHashKey();

            Assert.Same(message, result);
            Assert.Null(message.Envelope.HashKey);
        }
    }
}
