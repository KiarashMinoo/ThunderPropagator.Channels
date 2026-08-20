using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.BuildingBlocks.Application.Enums;
using ThunderPropagator.Channels.Chat;

namespace ThunderPropagator.UnitTests.Channels.Chat
{
    /// <summary>
    /// Issue #107: ChatChannelMetadata declared descriptor indices 0, 1, 3, and 4 — index 2 was
    /// missing entirely, along with any descriptor for ChatChannelFeederMessage.DateTime. A gap in
    /// the declared indices can break a positional metadata consumer, and the missing DateTime
    /// descriptor meant the message timestamp was absent from the schema altogether. A
    /// DateTimeChannelProgramsDescriptor (not TimeChannelProgramsDescriptor — DateTime here is a
    /// DateTimeOffset carrying the full message-creation instant, not a bare time-of-day component,
    /// so DataType.DateTime is the type that actually matches it) now fills index 2.
    /// </summary>
    public sealed class ChatChannelMetadataTests
    {
        private static ChatChannel CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(ChatChannelConfiguration)).Returns(new ChatChannelConfiguration());

            var channel = new ChatChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        [Fact]
        public void ChannelProgramsDescriptors_HasExactlyTenEntries()
        {
            var channel = CreateChannel();

            var descriptors = channel.Metadata.ChannelProgramsDescriptors.ToArray();

            Assert.Equal(10, descriptors.Length);
        }

        [Fact]
        public void ChannelProgramsDescriptors_IndicesAreContiguousFromZeroThroughNine()
        {
            var channel = CreateChannel();

            var indices = channel.Metadata.ChannelProgramsDescriptors.ToArray()
                .Select(descriptor => descriptor.Index)
                .OrderBy(index => index)
                .ToArray();

            Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], indices);
        }

        [Fact]
        public void ChannelProgramsDescriptors_HasNoDuplicateIndices()
        {
            var channel = CreateChannel();

            var descriptors = channel.Metadata.ChannelProgramsDescriptors.ToArray();

            Assert.Equal(descriptors.Length, descriptors.Select(descriptor => descriptor.Index).Distinct().Count());
        }

        [Fact]
        public void ChannelProgramsDescriptors_HasNoDuplicateNames()
        {
            var channel = CreateChannel();

            var descriptors = channel.Metadata.ChannelProgramsDescriptors.ToArray();

            Assert.Equal(descriptors.Length, descriptors.Select(descriptor => descriptor.Name).Distinct().Count());
        }

        [Theory]
        [InlineData(0, nameof(ChatChannelFeederMessage.UserId), DataType.String)]
        [InlineData(1, nameof(ChatChannelFeederMessage.SenderUserId), DataType.String)]
        [InlineData(2, nameof(ChatChannelFeederMessage.DateTime), DataType.DateTime)]
        [InlineData(3, nameof(ChatChannelFeederMessage.GroupId), DataType.String)]
        [InlineData(4, nameof(ChatChannelFeederMessage.Message), DataType.String)]
        [InlineData(5, nameof(ChatChannelFeederMessage.MessageId), DataType.String)]
        [InlineData(6, nameof(ChatChannelFeederMessage.IsDeleted), DataType.Boolean)]
        [InlineData(7, nameof(ChatChannelFeederMessage.IsEdited), DataType.Boolean)]
        [InlineData(8, nameof(ChatChannelFeederMessage.IsOffline), DataType.Boolean)]
        [InlineData(9, nameof(ChatChannelFeederMessage.IsGroupDeleted), DataType.Boolean)]
        public void ChannelProgramsDescriptors_EachFieldIsRegisteredAtItsExpectedIndexWithItsExpectedType(int expectedIndex, string fieldName, DataType expectedType)
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[fieldName];

            Assert.Equal(expectedIndex, descriptor.Index);
            Assert.Equal(expectedType, descriptor.Type);
        }

        [Fact]
        public void DateTime_UsesADateTimeDescriptor_NotABareTimeDescriptor()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(ChatChannelFeederMessage.DateTime)];

            Assert.IsType<ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes.DateTimeChannelProgramsDescriptor>(descriptor);
        }

        [Fact]
        public void IsDeleted_UsesABooleanDescriptor()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(ChatChannelFeederMessage.IsDeleted)];

            Assert.IsType<ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes.BooleanChannelProgramsDescriptor>(descriptor);
        }

        [Fact]
        public void IsEdited_UsesABooleanDescriptor()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(ChatChannelFeederMessage.IsEdited)];

            Assert.IsType<ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes.BooleanChannelProgramsDescriptor>(descriptor);
        }

        [Fact]
        public void IsOffline_UsesABooleanDescriptor()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(ChatChannelFeederMessage.IsOffline)];

            Assert.IsType<ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes.BooleanChannelProgramsDescriptor>(descriptor);
        }

        [Fact]
        public void IsGroupDeleted_UsesABooleanDescriptor()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(ChatChannelFeederMessage.IsGroupDeleted)];

            Assert.IsType<ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes.BooleanChannelProgramsDescriptor>(descriptor);
        }

        [Fact]
        public void UserId_RemainsTheOnlySubscribingKey()
        {
            var channel = CreateChannel();

            var subscribingKeyNames = channel.Metadata.ChannelProgramsDescriptors.SubscribingKeys
                .Select(descriptor => descriptor.Name)
                .ToArray();

            Assert.Equal([nameof(ChatChannelFeederMessage.UserId)], subscribingKeyNames);
        }
    }
}
