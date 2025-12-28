using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ThunderPropagator.UnitTests.Demo.Airport
{
    public class AirportExtensionsTests
    {
        [Fact]
        public void AddAirportDemoChannel_DoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddAirportDemoChannel();
            Assert.NotNull(services);
        }
    }
}
