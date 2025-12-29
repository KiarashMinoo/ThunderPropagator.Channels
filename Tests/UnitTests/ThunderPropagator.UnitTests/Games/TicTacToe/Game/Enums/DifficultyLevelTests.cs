using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.Game.Enums
{
    public class DifficultyLevelTests
    {
        [Fact]
        public void DifficultyLevel_IsPublic()
        {
            var type = typeof(DifficultyLevel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void DifficultyLevel_IsEnum()
        {
            var type = typeof(DifficultyLevel);
            Assert.True(type.IsEnum);
        }

        [Theory]
        [InlineData(DifficultyLevel.Easy)]
        [InlineData(DifficultyLevel.Medium)]
        [InlineData(DifficultyLevel.Hard)]
        public void DifficultyLevel_HasExpectedValues(DifficultyLevel difficultyLevel)
        {
            Assert.True(Enum.IsDefined(typeof(DifficultyLevel), difficultyLevel));
        }

        [Fact]
        public void DifficultyLevel_HasThreeValues()
        {
            var values = Enum.GetValues<DifficultyLevel>();
            Assert.Equal(3, values.Length);
        }
    }
}
