using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ThunderPropagator.UnitTests.Channels.Clock
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
