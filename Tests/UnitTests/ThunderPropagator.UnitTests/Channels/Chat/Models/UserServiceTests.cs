﻿using Bogus;
using ThunderPropagator.Channels.Chat.Models.Users;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    public class UserServiceTests
    {
        private readonly Faker _faker = new();

        [Fact]
        public void User_IsPublic()
        {
            var type = typeof(User);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void UserNotFoundException_IsException()
        {
            var type = typeof(UserNotFoundException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void User_HasIdProperty()
        {
            var type = typeof(User);
            var property = type.GetProperty("Id");
            Assert.NotNull(property);
            Assert.Equal(typeof(Guid), property.PropertyType);
        }

        [Fact]
        public void User_HasUserNameProperty()
        {
            var type = typeof(User);
            var property = type.GetProperty("UserName");
            Assert.NotNull(property);
            Assert.Equal(typeof(string), property.PropertyType);
        }

        [Fact]
        public void User_HasPasswordHashProperty()
        {
            var type = typeof(User);
            var property = type.GetProperty("PasswordHash");
            Assert.NotNull(property);
            Assert.Equal(typeof(string), property.PropertyType);
        }

        [Fact]
        public void User_HasNameProperty()
        {
            var type = typeof(User);
            var property = type.GetProperty("Name");
            Assert.NotNull(property);
            Assert.Equal(typeof(string), property.PropertyType);
        }
    }
}
