using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Channels.Demo.VideoPlayer;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer
{
    /// <summary>
    /// Issue #215's own AC: "Metadata indices/types are stable and tested," plus a dedicated regression
    /// guard for "No client-facing state exposes the original source location."
    /// </summary>
    public sealed class VideoPlayerChannelMetadataTests
    {
        private static VideoPlayerChannel CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(VideoPlayerChannelConfiguration)).Returns(new VideoPlayerChannelConfiguration());

            var channel = new VideoPlayerChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        [Fact]
        public void ChannelProgramsDescriptors_HasExactlyThirteenEntries()
        {
            var channel = CreateChannel();

            var descriptors = channel.Metadata.ChannelProgramsDescriptors.ToArray();

            Assert.Equal(13, descriptors.Length);
        }

        [Fact]
        public void ChannelProgramsDescriptors_IndicesAreContiguousFromZeroThroughTwelve()
        {
            var channel = CreateChannel();

            var indices = channel.Metadata.ChannelProgramsDescriptors.ToArray()
                .Select(descriptor => descriptor.Index)
                .OrderBy(index => index)
                .ToArray();

            Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12], indices);
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
        [InlineData(0, nameof(VideoPlayerChannelFeederMessage.SessionId))]
        [InlineData(1, nameof(VideoPlayerChannelFeederMessage.VideoId))]
        [InlineData(2, nameof(VideoPlayerChannelFeederMessage.Title))]
        [InlineData(3, nameof(VideoPlayerChannelFeederMessage.State))]
        [InlineData(4, nameof(VideoPlayerChannelFeederMessage.Epoch))]
        [InlineData(5, nameof(VideoPlayerChannelFeederMessage.CurrentFrameNumber))]
        [InlineData(6, nameof(VideoPlayerChannelFeederMessage.MediaPosition))]
        [InlineData(7, nameof(VideoPlayerChannelFeederMessage.SyncTime))]
        [InlineData(8, nameof(VideoPlayerChannelFeederMessage.Host))]
        [InlineData(9, nameof(VideoPlayerChannelFeederMessage.ViewerCount))]
        [InlineData(10, nameof(VideoPlayerChannelFeederMessage.Duration))]
        [InlineData(11, nameof(VideoPlayerChannelFeederMessage.SourceFrameRate))]
        [InlineData(12, nameof(VideoPlayerChannelFeederMessage.Reactions))]
        public void ChannelProgramsDescriptors_EachFieldIsRegisteredAtItsExpectedIndex(int expectedIndex, string fieldName)
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[fieldName];

            Assert.Equal(expectedIndex, descriptor.Index);
        }

        [Fact]
        public void SessionId_IsTheOnlySubscribingKey()
        {
            var channel = CreateChannel();

            var subscribingKeyNames = channel.Metadata.ChannelProgramsDescriptors.SubscribingKeys
                .Select(descriptor => descriptor.Name)
                .ToArray();

            Assert.Equal([nameof(VideoPlayerChannelFeederMessage.SessionId)], subscribingKeyNames);
        }

        [Fact]
        public void State_UsesAnEnumDescriptor()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(VideoPlayerChannelFeederMessage.State)];

            Assert.IsType<EnumChannelProgramsDescriptor<PlayState>>(descriptor);
        }

        [Theory]
        [InlineData(nameof(VideoPlayerChannelFeederMessage.Epoch))]
        [InlineData(nameof(VideoPlayerChannelFeederMessage.CurrentFrameNumber))]
        [InlineData(nameof(VideoPlayerChannelFeederMessage.MediaPosition))]
        [InlineData(nameof(VideoPlayerChannelFeederMessage.SyncTime))]
        [InlineData(nameof(VideoPlayerChannelFeederMessage.ViewerCount))]
        [InlineData(nameof(VideoPlayerChannelFeederMessage.Duration))]
        public void EpochFrameAndTimingFields_UseANumericDescriptor(string fieldName)
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[fieldName];

            Assert.IsType<NumberChannelProgramsDescriptor>(descriptor);
        }

        [Fact]
        public void Reactions_UsesAJsonDescriptor()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(VideoPlayerChannelFeederMessage.Reactions)];

            Assert.IsType<JsonChannelProgramsDescriptor>(descriptor);
        }

        // Issue #215's own AC: "No client-facing state exposes the original source location." A
        // reflection-based guard rather than a one-off assertion, so a future field added to this
        // message without reading this comment still gets caught by name.
        [Theory]
        [InlineData("Url")]
        [InlineData("Uri")]
        [InlineData("Path")]
        [InlineData("Credential")]
        [InlineData("Secret")]
        [InlineData("Token")]
        [InlineData("ApiKey")]
        [InlineData("ConnectionString")]
        public void FeederMessageProperties_NeverNameASourceLocationOrCredential(string forbiddenSubstring)
        {
            var propertyNames = typeof(VideoPlayerChannelFeederMessage)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name);

            Assert.DoesNotContain(propertyNames, name => name.Contains(forbiddenSubstring, StringComparison.OrdinalIgnoreCase));
        }
    }
}
