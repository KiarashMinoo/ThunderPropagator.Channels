﻿using Xunit;

namespace ThunderPropagator.UnitTests.Games.RockPaperScissors
{
    public class RockPaperScissorsChannelTests
    {
        [Fact]
        public void RockPaperScissorsChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.RockPaperScissors.RockPaperScissorsChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void RockPaperScissorsChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.RockPaperScissors.RockPaperScissorsChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void RockPaperScissorsChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.RockPaperScissors.RockPaperScissorsChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void RockPaperScissorsChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Games.RockPaperScissors.RockPaperScissorsChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void MoveKind_IsEnum()
        {
            var type = typeof(ThunderPropagator.Channels.Games.RockPaperScissors.MoveKind);
            Assert.True(type.IsEnum);
        }

        [Fact]
        public void PlayerType_IsEnum()
        {
            var type = typeof(ThunderPropagator.Channels.Games.RockPaperScissors.PlayerType);
            Assert.True(type.IsEnum);
        }

        [Fact]
        public void Player_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Games.RockPaperScissors.Player);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void RockPaperScissorsComputer_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Games.RockPaperScissors.RockPaperScissorsComputer);
            Assert.True(type.IsNotPublic);
        }
    }
}

