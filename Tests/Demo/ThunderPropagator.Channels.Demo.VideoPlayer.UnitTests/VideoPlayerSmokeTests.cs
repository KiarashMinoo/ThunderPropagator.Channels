using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.UnitTests
{
    public class VideoPlayerSmokeTests
    {
        [Fact]
        public void Compiles()
        {
            Assert.True(true);
        }

        // Issue #213: proves the scaffold itself — an empty VideoPlayerChannelFeederMessage and an
        // empty VideoPlayerChannelMetadata.ChannelProgramsDescriptors — actually constructs and
        // initializes without throwing, per the AC's "no placeholder runtime exceptions remain in
        // registered components". Mirrors QuizSmokeTests' own CreateChannel pattern from #183.
        [Fact]
        public void VideoPlayerChannel_ConstructsAndInitializes_WithoutThrowing()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(VideoPlayerChannelConfiguration)).Returns(new VideoPlayerChannelConfiguration());

            var channel = new VideoPlayerChannel(serviceProvider);
            var exception = Record.Exception(() => channel.Initialize(CancellationToken.None));

            Assert.Null(exception);
        }

        [Fact]
        public void AddVideoPlayerChannel_DoesNotThrow()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() => services.AddVideoPlayerChannel());

            Assert.Null(exception);
        }
    }
}
