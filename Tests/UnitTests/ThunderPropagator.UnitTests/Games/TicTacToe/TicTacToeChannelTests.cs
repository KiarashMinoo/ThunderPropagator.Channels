﻿using Xunit;

namespace ThunderPropagator.UnitTests.Games.TicTacToe
{
    public class TicTacToeChannelTests
    {
        [Fact]
        public void TicTacToeChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.TicTacToeChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TicTacToeChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.TicTacToeChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TicTacToeChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.TicTacToeChannelMetadata);
            Assert.True(type.IsPublic);
        }
    }
}

