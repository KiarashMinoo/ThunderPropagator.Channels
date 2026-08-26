using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Throughput.Extensions;

namespace ThunderPropagator.Channels.Throughput.UnitTests.Extensions
{
    public class ThroughputExtensionsTests
    {
        [Fact]
        public void AddThroughputChannel_DoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddThroughputChannel();
            Assert.NotNull(services);
        }
    }
}
