using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ThunderPropagator.UnitTests.Channels.ResourceMonitoring
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
