using Microsoft.Extensions.DependencyInjection;

namespace ThunderPropagator.Channels.ResourceMonitoring.UnitTests
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
