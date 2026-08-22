using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ThunderPropagator.Channels.Demo.Quiz.UnitTests
{
    public class QuizSmokeTests
    {
        [Fact]
        public void Compiles()
        {
            Assert.True(true);
        }

        // Issue #183: proves the scaffold itself — an empty QuizChannelFeederMessage and an empty
        // QuizChannelMetadata.ChannelProgramsDescriptors — actually constructs and initializes without
        // throwing, per the AC's "no placeholder runtime exceptions remain in the scaffold". Mirrors
        // ChatChannelMetadataTests/ChatChannelAuthenticationTests' own CreateChannel pattern.
        [Fact]
        public void QuizChannel_ConstructsAndInitializes_WithoutThrowing()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(QuizChannelConfiguration)).Returns(new QuizChannelConfiguration());

            var channel = new QuizChannel(serviceProvider);
            var exception = Record.Exception(() => channel.Initialize(CancellationToken.None));

            Assert.Null(exception);
        }

        [Fact]
        public void AddQuizChannel_DoesNotThrow()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() => services.AddQuizChannel());

            Assert.Null(exception);
        }
    }
}
