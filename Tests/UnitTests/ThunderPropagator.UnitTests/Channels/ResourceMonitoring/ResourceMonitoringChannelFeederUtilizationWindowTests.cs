using NSubstitute;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.BuildingBlocks.Infrastructure.SystemResourceMonitor;
using ThunderPropagator.Channels.ResourceMonitoring;
using ThunderPropagator.UnitTests.Feeders;
using ThunderPropagator.Channels.ResourceMonitoring.Channel;
using ThunderPropagator.Channels.ResourceMonitoring.Configuration;
using ThunderPropagator.Channels.ResourceMonitoring.Feeders;
using ThunderPropagator.Channels.ResourceMonitoring.Messages;

namespace ThunderPropagator.UnitTests.Channels.ResourceMonitoring
{
    public sealed class ResourceMonitoringChannelFeederUtilizationWindowTests
    {
        private static ResourceMonitoringChannelFeeder CreateFeeder(int utilizationWindow)
        {
            var serviceProvider = FeederCancellationTestHelper.BuildServiceProvider<ResourceMonitoringChannelFeederMessage, ResourceMonitoringChannelFeederConfiguration>();
            serviceProvider.RegisterChannelConfiguration(new ResourceMonitoringChannelConfiguration());
            serviceProvider.RegisterService(Substitute.For<ISystemResourceMonitor>());

            var channel = new ResourceMonitoringChannel(serviceProvider);
            var feederConfiguration = new ResourceMonitoringChannelFeederConfiguration { UtilizationWindow = utilizationWindow };
            var feederHandler = new NoOpFeederHandler<ResourceMonitoringChannel, ResourceMonitoringChannelFeederMessage>();

            return new ResourceMonitoringChannelFeeder(channel, feederConfiguration, feederHandler, serviceProvider);
        }

        private static long GetWindowMilliseconds(ResourceMonitoringChannelFeeder feeder)
        {
            var field = typeof(ResourceMonitoringChannelFeeder).GetField("_window", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException(typeof(ResourceMonitoringChannelFeeder).FullName, "_window");
            return (long)field.GetValue(feeder)!;
        }

        [Fact]
        public void Constructor_WithNormalValue_ConvertsSecondsToMillisecondsExactly()
        {
            var feeder = CreateFeeder(30);

            Assert.Equal(30_000L, GetWindowMilliseconds(feeder));
        }

        [Fact]
        public void Constructor_AtSupportedUpperBoundary_ConvertsSecondsToMillisecondsExactly()
        {
            var feeder = CreateFeeder(ResourceMonitoringChannelFeeder.MaxUtilizationWindowSeconds);

            Assert.Equal((long)ResourceMonitoringChannelFeeder.MaxUtilizationWindowSeconds * 1000L, GetWindowMilliseconds(feeder));
        }

        [Fact]
        public void Constructor_JustBeyondSupportedUpperBoundary_ThrowsIdentifyingUtilizationWindow()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateFeeder(ResourceMonitoringChannelFeeder.MaxUtilizationWindowSeconds + 1));

            Assert.Equal(nameof(ResourceMonitoringChannelFeederConfiguration.UtilizationWindow), exception.ParamName);
        }

        [Fact]
        public void Constructor_WithOverflowingValue_ThrowsIdentifyingUtilizationWindow_InsteadOfWrappingNegative()
        {
            // Before the fix, int.MaxValue * 1000 overflowed silently in 32-bit arithmetic and produced
            // a negative _window, which Task.Delay would only reject later, deep in the feeder loop.
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateFeeder(int.MaxValue));

            Assert.Equal(nameof(ResourceMonitoringChannelFeederConfiguration.UtilizationWindow), exception.ParamName);
        }

        [Fact]
        public void Constructor_WithZeroOrNegativeValue_ThrowsIdentifyingUtilizationWindow()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateFeeder(0));

            Assert.Equal(nameof(ResourceMonitoringChannelFeederConfiguration.UtilizationWindow), exception.ParamName);
        }
    }
}
