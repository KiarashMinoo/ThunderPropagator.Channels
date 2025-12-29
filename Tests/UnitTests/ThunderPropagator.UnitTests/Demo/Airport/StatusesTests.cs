using ThunderPropagator.Channels.Demo.Airport;

namespace ThunderPropagator.UnitTests.Demo.Airport
{
    public class StatusesTests
    {
        [Fact]
        public void Statuses_IsPublic()
        {
            var type = typeof(Statuses);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void Statuses_IsEnum()
        {
            var type = typeof(Statuses);
            Assert.True(type.IsEnum);
        }

        [Theory]
        [InlineData(Statuses.ScheduledOnTime)]
        [InlineData(Statuses.ScheduledDelayed)]
        [InlineData(Statuses.EnRouteOnTime)]
        [InlineData(Statuses.EnRouteDelayed)]
        [InlineData(Statuses.LandedOnTime)]
        [InlineData(Statuses.LandedDelayed)]
        [InlineData(Statuses.Cancelled)]
        [InlineData(Statuses.Deleted)]
        public void Statuses_HasExpectedValues(Statuses status)
        {
            Assert.True(Enum.IsDefined(typeof(Statuses), status));
        }

        [Fact]
        public void Statuses_HasEightValues()
        {
            var values = Enum.GetValues<Statuses>();
            Assert.Equal(8, values.Length);
        }
    }
}
