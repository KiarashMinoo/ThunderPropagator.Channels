using Microsoft.Extensions.DependencyInjection;

namespace ThunderPropagator.Channels.NetworkMonitoring.UnitTests
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
