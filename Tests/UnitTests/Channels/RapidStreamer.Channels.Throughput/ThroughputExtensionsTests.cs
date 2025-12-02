using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RapidStreamer.UnitTests.Channels.Throughput
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
