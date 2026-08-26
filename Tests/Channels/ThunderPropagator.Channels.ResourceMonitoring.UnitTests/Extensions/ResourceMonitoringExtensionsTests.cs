using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.ResourceMonitoring.Extensions;

namespace ThunderPropagator.Channels.ResourceMonitoring.UnitTests.Extensions
{
    public class ResourceMonitoringExtensionsTests
    {
        [Fact]
        public void AddResourceMonitoringChannel_DoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddResourceMonitoringChannel();
            Assert.NotNull(services);
        }
    }
}
