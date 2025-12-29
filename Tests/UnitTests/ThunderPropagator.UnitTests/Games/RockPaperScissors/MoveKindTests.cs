using ThunderPropagator.Channels.Games.RockPaperScissors;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors
{
    public class MoveKindTests
    {
        [Fact]
        public void MoveKind_IsPublic()
        {
            var type = typeof(MoveKind);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void MoveKind_IsEnum()
        {
            var type = typeof(MoveKind);
            Assert.True(type.IsEnum);
        }

        [Theory]
        [InlineData(MoveKind.Rock, 1)]
        [InlineData(MoveKind.Paper, 2)]
        [InlineData(MoveKind.Scissor, 3)]
        public void MoveKind_HasExpectedValues(MoveKind move, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)move);
        }

        [Fact]
        public void MoveKind_HasThreeValues()
        {
            var values = Enum.GetValues<MoveKind>();
            Assert.Equal(3, values.Length);
        }
    }
}
