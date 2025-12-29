﻿using Bogus;
using ThunderPropagator.Channels.Chat.Models.Groups;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    public class GroupServiceTests
    {
        private readonly Faker _faker = new();

        [Fact]
        public void Group_IsPublic()
        {
            var type = typeof(Group);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void GroupNotFoundException_IsException()
        {
            var type = typeof(GroupNotFoundException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void GroupUser_IsPublic()
        {
            var type = typeof(GroupUser);
            Assert.True(type.IsPublic);
        }
    }
}
