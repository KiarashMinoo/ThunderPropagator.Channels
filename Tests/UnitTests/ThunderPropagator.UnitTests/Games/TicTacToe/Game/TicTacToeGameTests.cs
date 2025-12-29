﻿using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.Game
{
    public class TicTacToeGameTests
    {
        [Fact]
        public void InvalidMoveException_IsException()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.Game.Exceptions.InvalidMoveException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void PlayerSign_IsEnum()
        {
            var type = typeof(PlayerSign);
            Assert.True(type.IsEnum);
        }

        [Fact]
        public void PlayerKind_IsEnum()
        {
            var type = typeof(PlayerKind);
            Assert.True(type.IsEnum);
        }

        [Fact]
        public void DifficultyLevel_IsEnum()
        {
            var type = typeof(DifficultyLevel);
            Assert.True(type.IsEnum);
        }
    }
}
