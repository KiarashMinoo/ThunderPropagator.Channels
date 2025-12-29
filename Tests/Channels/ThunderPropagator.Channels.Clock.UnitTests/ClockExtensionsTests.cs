using Microsoft.Extensions.DependencyInjection;

namespace ThunderPropagator.Channels.Clock.UnitTests
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
