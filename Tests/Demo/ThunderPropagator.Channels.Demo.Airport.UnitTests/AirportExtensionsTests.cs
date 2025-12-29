using Microsoft.Extensions.DependencyInjection;

namespace ThunderPropagator.Channels.Demo.Airport.UnitTests
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
