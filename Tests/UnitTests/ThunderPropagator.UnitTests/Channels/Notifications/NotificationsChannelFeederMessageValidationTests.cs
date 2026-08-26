using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #68: Id and Subject were silently accepted empty, producing notifications that
    /// couldn't be reliably identified or presented. Explicit null/empty/whitespace/over-length
    /// values are now rejected immediately by the Id/Subject setters (covering the dictionary and
    /// copy constructors too, since both write the payload directly and then re-validate); a
    /// property that's simply never touched is instead caught at the channel's emission boundary,
    /// since setter-level validation alone can't observe a property that was never assigned.
    /// </summary>
    public sealed class NotificationsChannelFeederMessageValidationTests
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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Id_Setter_RejectsNullEmptyOrWhitespace(string? invalidValue)
        {
            var exception = Assert.Throws<NotificationsChannelFeederMessageValidationException>(
                () => new NotificationsChannelFeederMessage { Id = invalidValue! });

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Id), exception.PropertyName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Subject_Setter_RejectsNullEmptyOrWhitespace(string? invalidValue)
        {
            var exception = Assert.Throws<NotificationsChannelFeederMessageValidationException>(
                () => new NotificationsChannelFeederMessage { Subject = invalidValue! });

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Subject), exception.PropertyName);
        }

        [Fact]
        public void Id_Setter_AcceptsAValueAtExactlyTheMaxLength()
        {
            var value = new string('a', NotificationsChannelFeederMessage.IdMaxLength);

            var message = new NotificationsChannelFeederMessage { Id = value };

            Assert.Equal(value, message.Id);
        }

        [Fact]
        public void Id_Setter_RejectsAValueOneOverTheMaxLength()
        {
            var value = new string('a', NotificationsChannelFeederMessage.IdMaxLength + 1);

            var exception = Assert.Throws<NotificationsChannelFeederMessageValidationException>(
                () => new NotificationsChannelFeederMessage { Id = value });

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Id), exception.PropertyName);
        }

        [Fact]
        public void Subject_Setter_AcceptsAValueAtExactlyTheMaxLength()
        {
            var value = new string('a', NotificationsChannelFeederMessage.SubjectMaxLength);

            var message = new NotificationsChannelFeederMessage { Subject = value };

            Assert.Equal(value, message.Subject);
        }

        [Fact]
        public void Subject_Setter_RejectsAValueOneOverTheMaxLength()
        {
            var value = new string('a', NotificationsChannelFeederMessage.SubjectMaxLength + 1);

            var exception = Assert.Throws<NotificationsChannelFeederMessageValidationException>(
                () => new NotificationsChannelFeederMessage { Subject = value });

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Subject), exception.PropertyName);
        }

        [Fact]
        public void ValidId_And_ValidSubject_RemainAcceptedThroughEveryConstructionPath()
        {
            var direct = new NotificationsChannelFeederMessage { Id = "notification-1", Subject = "subject" };
            Assert.Equal("notification-1", direct.Id);
            Assert.Equal("subject", direct.Subject);

            var copy = new NotificationsChannelFeederMessage(direct);
            Assert.Equal("notification-1", copy.Id);
            Assert.Equal("subject", copy.Subject);

            IDictionary<string, object?> raw = new Dictionary<string, object?>
            {
                [nameof(NotificationsChannelFeederMessage.Id)] = "notification-2",
                [nameof(NotificationsChannelFeederMessage.Subject)] = "subject-2"
            };
            var fromDictionary = new NotificationsChannelFeederMessage(raw);
            Assert.Equal("notification-2", fromDictionary.Id);
            Assert.Equal("subject-2", fromDictionary.Subject);
        }

        [Fact]
        public void DictionaryConstruction_WithAMissingId_ThrowsRatherThanBypassingValidation()
        {
            IDictionary<string, object?> raw = new Dictionary<string, object?>
            {
                [nameof(NotificationsChannelFeederMessage.Subject)] = "subject"
            };

            var exception = Assert.Throws<NotificationsChannelFeederMessageValidationException>(
                () => new NotificationsChannelFeederMessage(raw));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Id), exception.PropertyName);
        }

        [Fact]
        public void DictionaryConstruction_WithAnOverLengthSubject_ThrowsRatherThanBypassingValidation()
        {
            IDictionary<string, object?> raw = new Dictionary<string, object?>
            {
                [nameof(NotificationsChannelFeederMessage.Id)] = "notification-1",
                [nameof(NotificationsChannelFeederMessage.Subject)] = new string('a', NotificationsChannelFeederMessage.SubjectMaxLength + 1)
            };

            var exception = Assert.Throws<NotificationsChannelFeederMessageValidationException>(
                () => new NotificationsChannelFeederMessage(raw));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Subject), exception.PropertyName);
        }

        [Fact]
        public async Task Emission_OfAMessageThatNeverSetId_ThrowsAtTheEmissionBoundary()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { UserId = "user-1", Subject = "subject" };

            var exception = await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Id), exception.PropertyName);
        }

        [Fact]
        public async Task Emission_OfAMessageThatNeverSetSubject_ThrowsAtTheEmissionBoundary()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1" };

            var exception = await Assert.ThrowsAsync<NotificationsChannelFeederMessageValidationException>(
                () => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Equal(nameof(NotificationsChannelFeederMessage.Subject), exception.PropertyName);
        }

        [Fact]
        public async Task Emission_OfAFullyValidMessage_Succeeds()
        {
            var channel = CreateChannel();
            IChannel iChannel = channel;
            var message = new NotificationsChannelFeederMessage { UserId = "user-1", Id = "notification-1", Subject = "subject" };

            var exception = await Record.ExceptionAsync(() => iChannel.EmitMessageAsync(message, CancellationToken.None));

            Assert.Null(exception);
        }
    }
}
