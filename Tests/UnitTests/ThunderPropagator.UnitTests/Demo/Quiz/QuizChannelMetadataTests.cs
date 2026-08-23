using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Channels.Demo.Quiz;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.UnitTests.Demo.Quiz
{
    /// <summary>
    /// Issue #185: QuizChannelMetadata's field schema — GameId as the only subscribing key,
    /// contiguous/unique indices 0 through 9, Phase on the enum descriptor, TimeRemaining/
    /// QuestionIndex/TotalQuestions on the numeric descriptor, and Options/Scoreboard on the JSON
    /// descriptor.
    /// </summary>
    public sealed class QuizChannelMetadataTests
    {
        private static QuizChannel CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(new QuizChannelConfiguration());
            serviceProvider.GetService(typeof(QuizGameSessionStore)).Returns(new QuizGameSessionStore());
            serviceProvider.GetService(typeof(QuizGameLoopRegistry)).Returns(new QuizGameLoopRegistry());

            var channel = new QuizChannel(serviceProvider);
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
        [InlineData(0, nameof(QuizChannelFeederMessage.GameId))]
        [InlineData(1, nameof(QuizChannelFeederMessage.Phase))]
        [InlineData(2, nameof(QuizChannelFeederMessage.QuestionText))]
        [InlineData(3, nameof(QuizChannelFeederMessage.Options))]
        [InlineData(4, nameof(QuizChannelFeederMessage.TimeRemaining))]
        [InlineData(5, nameof(QuizChannelFeederMessage.QuestionIndex))]
        [InlineData(6, nameof(QuizChannelFeederMessage.TotalQuestions))]
        [InlineData(7, nameof(QuizChannelFeederMessage.Scoreboard))]
        [InlineData(8, nameof(QuizChannelFeederMessage.CorrectAnswer))]
        [InlineData(9, nameof(QuizChannelFeederMessage.Winner))]
        public void ChannelProgramsDescriptors_EachFieldIsRegisteredAtItsExpectedIndex(int expectedIndex, string fieldName)
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[fieldName];

            Assert.Equal(expectedIndex, descriptor.Index);
        }

        [Fact]
        public void GameId_IsTheOnlySubscribingKey()
        {
            var channel = CreateChannel();

            var subscribingKeyNames = channel.Metadata.ChannelProgramsDescriptors.SubscribingKeys
                .Select(descriptor => descriptor.Name)
                .ToArray();

            Assert.Equal([nameof(QuizChannelFeederMessage.GameId)], subscribingKeyNames);
        }

        [Fact]
        public void Phase_UsesAnEnumDescriptor()
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[nameof(QuizChannelFeederMessage.Phase)];

            Assert.IsType<EnumChannelProgramsDescriptor<QuizPhase>>(descriptor);
        }

        [Theory]
        [InlineData(nameof(QuizChannelFeederMessage.TimeRemaining))]
        [InlineData(nameof(QuizChannelFeederMessage.QuestionIndex))]
        [InlineData(nameof(QuizChannelFeederMessage.TotalQuestions))]
        public void TimingAndCountFields_UseANumericDescriptor(string fieldName)
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[fieldName];

            Assert.IsType<NumberChannelProgramsDescriptor>(descriptor);
        }

        [Theory]
        [InlineData(nameof(QuizChannelFeederMessage.Options))]
        [InlineData(nameof(QuizChannelFeederMessage.Scoreboard))]
        public void CollectionFields_UseAJsonDescriptor(string fieldName)
        {
            var channel = CreateChannel();

            var descriptor = channel.Metadata.ChannelProgramsDescriptors[fieldName];

            Assert.IsType<JsonChannelProgramsDescriptor>(descriptor);
        }
    }
}
