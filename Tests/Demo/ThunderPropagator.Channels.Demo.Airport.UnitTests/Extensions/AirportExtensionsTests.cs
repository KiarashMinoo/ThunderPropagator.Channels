using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Demo.Airport.Extensions;

namespace ThunderPropagator.Channels.Demo.Airport.UnitTests.Extensions
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
