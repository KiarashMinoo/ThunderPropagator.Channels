using Microsoft.Extensions.DependencyInjection;

namespace ThunderPropagator.Channels.Throughput.UnitTests
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
