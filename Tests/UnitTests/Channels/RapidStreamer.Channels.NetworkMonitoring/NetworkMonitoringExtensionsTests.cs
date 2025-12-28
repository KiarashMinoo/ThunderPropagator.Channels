using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ThunderPropagator.UnitTests.Channels.NetworkMonitoring
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
