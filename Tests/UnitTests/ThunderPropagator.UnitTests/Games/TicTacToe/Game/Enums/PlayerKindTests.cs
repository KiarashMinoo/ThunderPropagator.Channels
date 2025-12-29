using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.Game.Enums
{
    public class PlayerKindTests
    {
        [Fact]
        public void PlayerKind_IsPublic()
        {
            var type = typeof(PlayerKind);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PlayerKind_IsEnum()
        {
            var type = typeof(PlayerKind);
            Assert.True(type.IsEnum);
        }

        [Theory]
        [InlineData(PlayerKind.Human)]
        [InlineData(PlayerKind.Computer)]
        public void PlayerKind_HasExpectedValues(PlayerKind playerKind)
        {
            Assert.True(Enum.IsDefined(typeof(PlayerKind), playerKind));
        }

        [Fact]
        public void PlayerKind_HasTwoValues()
        {
            var values = Enum.GetValues<PlayerKind>();
            Assert.Equal(2, values.Length);
        }
    }
}
