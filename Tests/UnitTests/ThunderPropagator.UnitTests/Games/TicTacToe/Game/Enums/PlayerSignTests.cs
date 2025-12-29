using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.Game.Enums
{
    public class PlayerSignTests
    {
        [Fact]
        public void PlayerSign_IsPublic()
        {
            var type = typeof(PlayerSign);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PlayerSign_IsEnum()
        {
            var type = typeof(PlayerSign);
            Assert.True(type.IsEnum);
        }

        [Theory]
        [InlineData(PlayerSign.X)]
        [InlineData(PlayerSign.O)]
        public void PlayerSign_HasExpectedValues(PlayerSign playerSign)
        {
            Assert.True(Enum.IsDefined(typeof(PlayerSign), playerSign));
        }

        [Fact]
        public void PlayerSign_HasTwoValues()
        {
            var values = Enum.GetValues<PlayerSign>();
            Assert.Equal(2, values.Length);
        }
    }
}
