using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Exceptions;
using ThunderPropagator.Channels.Notifications;
using ThunderPropagator.Channels.Notifications.Channel;
using ThunderPropagator.Channels.Notifications.Messages;

namespace ThunderPropagator.UnitTests.Channels.Notifications
{
    /// <summary>
    /// Issue #72: IsEnabled had no documented lifecycle contract, and the two emission paths
    /// disagreed on what disabling meant — broadcast emission threw ChannelIsNotEnabledException
    /// (via SearchSnapshotsAsync), while targeted (UserId-set) emission silently no-opped via
    /// base.EmitMessageAsync's early return. Both paths now throw consistently, checked explicitly
    /// at the very top of EmitMessageAsync, with a warning logged before the throw. IsEnabled is
    /// read fresh on every call (not cached), so toggling it takes effect on the very next
    /// operation — these tests cover both the disable-while-idle and re-enable-after-disable
    /// transitions.
    /// </summary>
    public sealed class NotificationsChannelIsEnabledContractTests
    {
        public sealed class TestNotificationsChannelConfiguration : AbstractChannelConfiguration;

        private sealed record CreatedChannel(
            NotificationsChannel<TestNotificationsChannelConfiguration> Channel,
            TestNotificationsChannelConfiguration Configuration,
            FakeLogCollector LogCollector);

        private static CreatedChannel CreateChannel(bool isEnabled)
        {
            var loggerProvider = new FakeLoggerProvider();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(loggerProvider));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(loggerFactory);
            var configuration = new TestNotificationsChannelConfiguration { IsEnabled = isEnabled };
            serviceProvider.GetService(typeof(TestNotificationsChannelConfiguration)).Returns(configuration);

            var channel = new NotificationsChannel<TestNotificationsChannelConfiguration>(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return new CreatedChannel(channel, configuration, loggerProvider.Collector);
        }

        private static NotificationsChannelFeederMessage ValidMessage(string? userId = null)
            => new() { UserId = userId, Id = "notification-1", Subject = "subject" };

        [Fact]
        public async Task TargetedEmission_WhileDisabled_ThrowsConsistentlyWithBroadcast()
        {
            var created = CreateChannel(isEnabled: false);
            IChannel iChannel = created.Channel;

            await Assert.ThrowsAsync<ChannelIsNotEnabledException>(
                () => iChannel.EmitMessageAsync(ValidMessage(userId: "user-1"), CancellationToken.None));
        }

        [Fact]
        public async Task BroadcastEmission_WhileDisabled_Throws()
        {
            var created = CreateChannel(isEnabled: false);
            IChannel iChannel = created.Channel;

            await Assert.ThrowsAsync<ChannelIsNotEnabledException>(
                () => iChannel.EmitMessageAsync(ValidMessage(), CancellationToken.None));
        }

        [Fact]
        public async Task RejectedEmission_WhileDisabled_LogsAWarningNamingTheChannel()
        {
            var created = CreateChannel(isEnabled: false);
            IChannel iChannel = created.Channel;

            await Assert.ThrowsAsync<ChannelIsNotEnabledException>(
                () => iChannel.EmitMessageAsync(ValidMessage(userId: "user-1"), CancellationToken.None));

            var warning = Assert.Single(created.LogCollector.GetSnapshot(), record => record.Level == LogLevel.Warning);
            Assert.Contains("disabled", warning.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DisableTransition_RejectsEmissionThatWouldHaveSucceededWhileEnabled()
        {
            var created = CreateChannel(isEnabled: true);
            IChannel iChannel = created.Channel;

            // Succeeds while enabled.
            await iChannel.EmitMessageAsync(ValidMessage(userId: "user-1"), CancellationToken.None);

            // IsEnabled is read fresh on every call, not cached, so toggling it off takes effect
            // immediately on the very next call — no restart or channel re-creation involved. This
            // mutates the same configuration instance the channel already holds a reference to.
            created.Configuration.IsEnabled = false;

            await Assert.ThrowsAsync<ChannelIsNotEnabledException>(
                () => iChannel.EmitMessageAsync(ValidMessage(userId: "user-1"), CancellationToken.None));
        }

        [Fact]
        public async Task ReEnableTransition_AllowsEmissionAgainAfterHavingBeenDisabled()
        {
            var created = CreateChannel(isEnabled: false);
            IChannel iChannel = created.Channel;

            await Assert.ThrowsAsync<ChannelIsNotEnabledException>(
                () => iChannel.EmitMessageAsync(ValidMessage(userId: "user-1"), CancellationToken.None));

            created.Configuration.IsEnabled = true;

            var exception = await Record.ExceptionAsync(
                () => iChannel.EmitMessageAsync(ValidMessage(userId: "user-1"), CancellationToken.None));

            Assert.Null(exception);
        }
    }
}
