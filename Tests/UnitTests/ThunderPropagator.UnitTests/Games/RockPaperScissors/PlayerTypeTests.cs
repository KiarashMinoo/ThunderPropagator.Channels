using ThunderPropagator.Channels.Games.RockPaperScissors;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors
{
    public class PlayerTypeTests
    {
        [Fact]
        public void PlayerType_IsPublic()
        {
            var type = typeof(PlayerType);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PlayerType_IsEnum()
        {
            var type = typeof(PlayerType);
            Assert.True(type.IsEnum);
        }

        [Theory]
        [InlineData(PlayerType.Human)]
        [InlineData(PlayerType.Computer)]
        public void PlayerType_HasExpectedValues(PlayerType playerType)
        {
            Assert.True(Enum.IsDefined(typeof(PlayerType), playerType));
        }

        [Fact]
        public void PlayerType_HasTwoValues()
        {
            var values = Enum.GetValues<PlayerType>();
            Assert.Equal(2, values.Length);
        }
    }
}
