﻿using NSubstitute;
using Xunit;

namespace ThunderPropagator.UnitTests.Channels.Chat
{
    public class ChatChannelTests
    {
        [Fact]
        public void ChatChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.ChatChannel);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.ChatChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.ChatChannelMetadata);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ChatChannelFeederMessage_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.ChatChannelFeederMessage);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void BaseChatContext_IsAbstract()
        {
            var type = typeof(ThunderPropagator.Channels.Chat.Models.BaseChatContext);
            Assert.True(type.IsAbstract);
        }
    }
}

