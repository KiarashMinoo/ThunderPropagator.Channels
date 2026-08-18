using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #69: NotificationsChannelFeederMessage.Type used to conflate content format (Text/Html)
    /// with semantic meaning, preventing categories like Warning or Error from being modeled at all.
    /// Type is now NotificationContentType-typed (renamed from the removed NotificationType, with
    /// identical underlying values — Text=0, Html=1 — so a consumer's own serialized numeric data
    /// stays valid once their code is updated to reference the new type name), and a new Category
    /// property (NotificationCategory) carries the semantic meaning independently.
    /// </summary>
    public sealed class NotificationsContentTypeAndCategoryTests
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
        [InlineData(NotificationContentType.Text, 0)]
        [InlineData(NotificationContentType.Html, 1)]
        public void NotificationContentType_UnderlyingValues_MatchTheRemovedNotificationTypesValues(NotificationContentType value, int expectedUnderlyingValue)
        {
            // The removed NotificationType enum was declared as `{ Text, Html }` with no explicit
            // numbering, so its implicit values were Text=0, Html=1 — this is the compatibility fact
            // consumers depend on: a stored/serialized numeric value written under the old type
            // means the same thing once read back as NotificationContentType.
            Assert.Equal(expectedUnderlyingValue, (int)value);
        }

        [Theory]
        [InlineData(NotificationCategory.Info)]
        [InlineData(NotificationCategory.Success)]
        [InlineData(NotificationCategory.Warning)]
        [InlineData(NotificationCategory.Error)]
        [InlineData(NotificationCategory.Alert)]
        [InlineData(NotificationCategory.System)]
        public void NotificationCategory_HasAllDocumentedValues(NotificationCategory value)
        {
            Assert.True(Enum.IsDefined(value));
        }

        [Fact]
        public void NotificationCategory_HasExactlySixValues()
        {
            Assert.Equal(6, Enum.GetValues<NotificationCategory>().Length);
        }

        [Theory]
        [InlineData(NotificationContentType.Text, NotificationCategory.Warning)]
        [InlineData(NotificationContentType.Html, NotificationCategory.Info)]
        [InlineData(NotificationContentType.Text, NotificationCategory.System)]
        public void ContentTypeAndCategory_AreIndependentlySelectable(NotificationContentType contentType, NotificationCategory category)
        {
            var message = new NotificationsChannelFeederMessage { Type = contentType, Category = category };

            Assert.Equal(contentType, message.Type);
            Assert.Equal(category, message.Category);
        }

        [Fact]
        public void Type_And_Category_DefaultIndependently()
        {
            var message = new NotificationsChannelFeederMessage();

            Assert.Equal(NotificationContentType.Text, message.Type);
            Assert.Equal(NotificationCategory.Info, message.Category);
        }

        [Theory]
        [InlineData(NotificationContentType.Text)]
        [InlineData(NotificationContentType.Html)]
        public void DictionaryConstruction_RoundTripsEveryContentTypeValue(NotificationContentType value)
        {
            IDictionary<string, object?> raw = new Dictionary<string, object?>
            {
                [nameof(NotificationsChannelFeederMessage.Id)] = "notification-1",
                [nameof(NotificationsChannelFeederMessage.Subject)] = "subject",
                [nameof(NotificationsChannelFeederMessage.Type)] = value
            };

            var message = new NotificationsChannelFeederMessage(raw);

            Assert.Equal(value, message.Type);
        }

        [Theory]
        [InlineData(NotificationCategory.Info)]
        [InlineData(NotificationCategory.Success)]
        [InlineData(NotificationCategory.Warning)]
        [InlineData(NotificationCategory.Error)]
        [InlineData(NotificationCategory.Alert)]
        [InlineData(NotificationCategory.System)]
        public void DictionaryConstruction_RoundTripsEveryCategoryValue(NotificationCategory value)
        {
            IDictionary<string, object?> raw = new Dictionary<string, object?>
            {
                [nameof(NotificationsChannelFeederMessage.Id)] = "notification-1",
                [nameof(NotificationsChannelFeederMessage.Subject)] = "subject",
                [nameof(NotificationsChannelFeederMessage.Category)] = value
            };

            var message = new NotificationsChannelFeederMessage(raw);

            Assert.Equal(value, message.Category);
        }

        [Fact]
        public void CopyConstructor_PreservesBothContentTypeAndCategoryIndependently()
        {
            var source = new NotificationsChannelFeederMessage
            {
                Id = "notification-1",
                Subject = "subject",
                Type = NotificationContentType.Html,
                Category = NotificationCategory.Error
            };

            var copy = new NotificationsChannelFeederMessage(source);

            Assert.Equal(NotificationContentType.Html, copy.Type);
            Assert.Equal(NotificationCategory.Error, copy.Category);
        }

        [Fact]
        public void ChannelMetadata_DeclaresBothTypeAndCategoryWithTheCorrectDescriptorTypes()
        {
            var channel = CreateChannel();

            var typeDescriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(NotificationsChannelFeederMessage.Type)];
            var categoryDescriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(NotificationsChannelFeederMessage.Category)];

            Assert.IsType<EnumChannelProgramsDescriptor<NotificationContentType>>(typeDescriptor);
            Assert.IsType<EnumChannelProgramsDescriptor<NotificationCategory>>(categoryDescriptor);
        }
    }
}
