using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Channels.Clock.Extensions;

namespace ThunderPropagator.Channels.Clock.UnitTests.Extensions
{
    public class ClockExtensionsTests
    {
        [Fact]
        public void AddClockChannel_DoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddClockChannel();
            Assert.NotNull(services);
        }
    }
}
