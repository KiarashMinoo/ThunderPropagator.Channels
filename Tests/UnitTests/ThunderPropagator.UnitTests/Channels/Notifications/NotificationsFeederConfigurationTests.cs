using Microsoft.Extensions.Configuration;
using ThunderPropagator.Channels.Notifications;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #62: NotificationsFeederConfiguration used to be an empty marker class. These tests
    /// cover the four new settings (BatchSize, DeduplicationWindow, TimeToLive,
    /// MaxDeliveryAttempts): their defaults preserve today's unbatched, non-deduplicated,
    /// non-expiring, no-retry behavior; invalid values are rejected with a clear message as soon as
    /// they're set (which covers both programmatic configuration and IConfiguration binding, since
    /// binding assigns through the same public setters); and IConfiguration-based binding — the
    /// mechanism AddNotificationsChannelFeeder(IConfigurationSection) actually uses — populates all
    /// four correctly.
    /// </summary>
    public sealed class NotificationsFeederConfigurationTests
    {
        public sealed class TestNotificationsFeederConfiguration : NotificationsFeederConfiguration;

        [Fact]
        public void Defaults_PreserveTodaysBehavior()
        {
            var configuration = new TestNotificationsFeederConfiguration();

            Assert.Equal(1, configuration.BatchSize);
            Assert.Equal(TimeSpan.Zero, configuration.DeduplicationWindow);
            Assert.Null(configuration.TimeToLive);
            Assert.Equal(1, configuration.MaxDeliveryAttempts);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void BatchSize_RejectsNonPositiveValues(int invalidValue)
        {
            var configuration = new TestNotificationsFeederConfiguration();

            var exception = Assert.Throws<ArgumentException>(() => configuration.BatchSize = invalidValue);

            Assert.Contains(nameof(NotificationsFeederConfiguration.BatchSize), exception.Message);
        }

        [Fact]
        public void DeduplicationWindow_RejectsNegativeValues()
        {
            var configuration = new TestNotificationsFeederConfiguration();

            var exception = Assert.Throws<ArgumentException>(() => configuration.DeduplicationWindow = TimeSpan.FromSeconds(-1));

            Assert.Contains(nameof(NotificationsFeederConfiguration.DeduplicationWindow), exception.Message);
        }

        [Fact]
        public void DeduplicationWindow_AllowsZero()
        {
            var configuration = new TestNotificationsFeederConfiguration { DeduplicationWindow = TimeSpan.Zero };

            Assert.Equal(TimeSpan.Zero, configuration.DeduplicationWindow);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TimeToLive_RejectsNonPositiveValues(int invalidSeconds)
        {
            var configuration = new TestNotificationsFeederConfiguration();

            var exception = Assert.Throws<ArgumentException>(() => configuration.TimeToLive = TimeSpan.FromSeconds(invalidSeconds));

            Assert.Contains(nameof(NotificationsFeederConfiguration.TimeToLive), exception.Message);
        }

        [Fact]
        public void TimeToLive_AllowsNull()
        {
            var configuration = new TestNotificationsFeederConfiguration { TimeToLive = TimeSpan.FromHours(1) };

            configuration.TimeToLive = null;

            Assert.Null(configuration.TimeToLive);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void MaxDeliveryAttempts_RejectsNonPositiveValues(int invalidValue)
        {
            var configuration = new TestNotificationsFeederConfiguration();

            var exception = Assert.Throws<ArgumentException>(() => configuration.MaxDeliveryAttempts = invalidValue);

            Assert.Contains(nameof(NotificationsFeederConfiguration.MaxDeliveryAttempts), exception.Message);
        }

        [Fact]
        public void ConfigurationBinding_PopulatesAllFourSettings()
        {
            var configuration = new TestNotificationsFeederConfiguration();

            var source = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [nameof(NotificationsFeederConfiguration.BatchSize)] = "25",
                    [nameof(NotificationsFeederConfiguration.DeduplicationWindow)] = "00:05:00",
                    [nameof(NotificationsFeederConfiguration.TimeToLive)] = "1.00:00:00",
                    [nameof(NotificationsFeederConfiguration.MaxDeliveryAttempts)] = "3"
                })
                .Build();

            source.Bind(configuration);

            Assert.Equal(25, configuration.BatchSize);
            Assert.Equal(TimeSpan.FromMinutes(5), configuration.DeduplicationWindow);
            Assert.Equal(TimeSpan.FromDays(1), configuration.TimeToLive);
            Assert.Equal(3, configuration.MaxDeliveryAttempts);
        }

        [Fact]
        public void ConfigurationBinding_WithAnInvalidValue_ThrowsRatherThanSilentlyAcceptingIt()
        {
            var configuration = new TestNotificationsFeederConfiguration();

            var source = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [nameof(NotificationsFeederConfiguration.BatchSize)] = "0"
                })
                .Build();

            // ConfigurationBinder sets properties via reflection, which wraps the guard clause's
            // ArgumentException in a TargetInvocationException — still a hard failure, not a
            // silently-accepted invalid value, which is the actual thing this test guards against.
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => source.Bind(configuration));

            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Contains(nameof(NotificationsFeederConfiguration.BatchSize), exception.InnerException.Message);
        }
    }
}
