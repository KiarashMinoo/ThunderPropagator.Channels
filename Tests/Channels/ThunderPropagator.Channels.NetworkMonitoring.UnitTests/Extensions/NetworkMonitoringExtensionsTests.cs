using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.NetworkMonitoring.Extensions;

namespace ThunderPropagator.Channels.NetworkMonitoring.UnitTests.Extensions
{
    public class NetworkMonitoringExtensionsTests
    {
        [Fact]
        public void AddNetworkMonitoringChannel_DoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddNetworkMonitoringChannel();
            Assert.NotNull(services);
        }
    }
}
