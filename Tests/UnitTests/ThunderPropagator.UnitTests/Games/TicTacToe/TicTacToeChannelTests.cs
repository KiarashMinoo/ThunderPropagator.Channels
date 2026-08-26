using ThunderPropagator.Channels.Games.TicTacToe.Channel;
using ThunderPropagator.Channels.Games.TicTacToe.Configuration;
using ThunderPropagator.Channels.Games.TicTacToe.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Games.TicTacToe
{
    public class TicTacToeChannelTests
    {
        [Fact]
        public void TicTacToeChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.Channel.TicTacToeChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TicTacToeChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.Configuration.TicTacToeChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TicTacToeChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.Metadata.TicTacToeChannelMetadata);
            Assert.True(type.IsPublic);
        }
    }
}

