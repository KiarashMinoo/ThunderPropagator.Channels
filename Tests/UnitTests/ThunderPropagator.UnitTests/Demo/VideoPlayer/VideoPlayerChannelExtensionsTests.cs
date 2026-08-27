using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Configuration;
using ThunderPropagator.Channels.Demo.VideoPlayer.Extensions;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Diagnostics;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Playlist;

namespace ThunderPropagator.UnitTests.Demo.VideoPlayer
{
    /// <summary>
    /// #238's own scope, "one extension call registers all required server-side services" — covers
    /// <see cref="VideoPlayerChannelExtensions.AddVideoPlayerChannel"/>'s own DI-registration contract
    /// directly, mirroring <c>QuizChannelExtensionsTests</c>' own established pattern for the same kind
    /// of ticket elsewhere in this codebase.
    /// </summary>
    public sealed class VideoPlayerChannelExtensionsTests
    {
        // AddChannel<TChannel>'s own factory needs IHostApplicationLifetime and ILoggerFactory
        // resolvable to construct and initialize the channel — registering them for real here is what
        // makes GetRequiredService<VideoPlayerChannel>() below an actual end-to-end resolution through
        // AddVideoPlayerChannel, not just a registration check.
        private static IServiceCollection CreateHostServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(Substitute.For<IHostApplicationLifetime>());
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            return services;
        }

        private static VideoPlaylistEntry CreateLocalEntry(string videoId, bool isEnabled = true) => new()
        {
            VideoId = videoId,
            Title = videoId,
            Source = new VideoSource { Location = Path.Combine(Path.GetTempPath(), $"{videoId}.mp4") },
            IsEnabled = isEnabled
        };

        private static VideoPlaylistPolicy LocalPolicy() => new() { LocalFileRoot = Path.GetTempPath() };

        [Fact]
        public void AddVideoPlayerChannel_ResolvesTheCompleteChannel()
        {
            var services = CreateHostServices();
            services.AddVideoPlayerChannel();
            var serviceProvider = services.BuildServiceProvider();

            var exception = Record.Exception(() => serviceProvider.GetRequiredService<VideoPlayerChannel>());

            Assert.Null(exception);
        }

        [Fact]
        public void AddVideoPlayerChannel_WithDefaultConfiguration_RegistersEveryPipelineIncludingReact()
        {
            var services = CreateHostServices();
            services.AddVideoPlayerChannel();
            var serviceProvider = services.BuildServiceProvider();

            var pipelines = serviceProvider.GetServices<IReceivePipeline<VideoPlayerChannel>>().ToList();

            Assert.Equal(6, pipelines.Count);
        }

        // #238's own scope: EnableReactions removes Video/React's own server runtime path entirely.
        [Fact]
        public void AddVideoPlayerChannel_WithReactionsDisabled_RegistersOnlyFivePipelines()
        {
            var services = CreateHostServices();
            services.AddVideoPlayerChannel(configuration => configuration.EnableReactions = false);
            var serviceProvider = services.BuildServiceProvider();

            var pipelines = serviceProvider.GetServices<IReceivePipeline<VideoPlayerChannel>>().ToList();

            Assert.Equal(5, pipelines.Count);
        }

        [Theory]
        [InlineData(typeof(VideoPlayerChannelConfiguration))]
        [InlineData(typeof(IVideoPlaylist))]
        [InlineData(typeof(VideoPlaybackTelemetry))]
        [InlineData(typeof(VideoPlaybackSessionManager))]
        public void AddVideoPlayerChannel_RegistersEveryRequiredService(Type serviceType)
        {
            var services = CreateHostServices();
            services.AddVideoPlayerChannel();
            var serviceProvider = services.BuildServiceProvider();

            var resolved = serviceProvider.GetService(serviceType);

            Assert.NotNull(resolved);
        }

        [Fact]
        public void AddVideoPlayerChannel_CalledTwice_DoesNotThrow()
        {
            var services = CreateHostServices();

            var exception = Record.Exception(() =>
            {
                services.AddVideoPlayerChannel();
                services.AddVideoPlayerChannel();
            });

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(typeof(VideoPlayerChannelConfiguration))]
        [InlineData(typeof(IVideoPlaylist))]
        [InlineData(typeof(VideoPlaybackTelemetry))]
        [InlineData(typeof(VideoPlaybackSessionManager))]
        public void AddVideoPlayerChannel_CalledTwice_RegistersEachSingletonExactlyOnce(Type serviceType)
        {
            var services = CreateHostServices();

            services.AddVideoPlayerChannel();
            services.AddVideoPlayerChannel();

            Assert.Single(services, descriptor => descriptor.ServiceType == serviceType);
        }

        [Fact]
        public void AddVideoPlayerChannel_CalledTwice_TheFirstCallsConfigurationWins()
        {
            var services = CreateHostServices();

            services.AddVideoPlayerChannel(configuration => configuration.MaxWidth = 320);
            services.AddVideoPlayerChannel(configuration => configuration.MaxWidth = 1920);
            var serviceProvider = services.BuildServiceProvider();

            Assert.Equal(320, serviceProvider.GetRequiredService<VideoPlayerChannelConfiguration>().MaxWidth);
        }

        [Fact]
        public void AddVideoPlayerChannel_ConfiguratorRuns_ResolvesTheConfiguredValues()
        {
            var services = CreateHostServices();

            services.AddVideoPlayerChannel(configuration =>
            {
                configuration.MaxWidth = 640;
                configuration.MaxHeight = 360;
                configuration.EnableAudio = false;
            });
            var serviceProvider = services.BuildServiceProvider();
            var resolved = serviceProvider.GetRequiredService<VideoPlayerChannelConfiguration>();

            Assert.Equal(640, resolved.MaxWidth);
            Assert.Equal(360, resolved.MaxHeight);
            Assert.False(resolved.EnableAudio);
        }

        [Fact]
        public void AddVideoPlayerChannel_WithAnInvalidSetting_Throws()
        {
            var services = CreateHostServices();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                services.AddVideoPlayerChannel(configuration => configuration.MaxWidth = 0));
        }

        // #234's own AC, extended by #238: DefaultVideoId is now always cross-checked against a real
        // playlist, not merely "when one happens to be supplied."
        [Fact]
        public void AddVideoPlayerChannel_WithDefaultVideoIdNotInThePlaylist_Throws()
        {
            var services = CreateHostServices();

            Assert.Throws<ArgumentException>(() =>
                services.AddVideoPlayerChannel(configuration => configuration.DefaultVideoId = "does-not-exist"));
        }

        [Fact]
        public void AddVideoPlayerChannel_WithDefaultVideoIdMatchingAnEnabledPlaylistEntry_DoesNotThrow()
        {
            var services = CreateHostServices();

            var exception = Record.Exception(() =>
                services.AddVideoPlayerChannel(configuration =>
                {
                    configuration.PlaylistPolicy = LocalPolicy();
                    configuration.PlaylistEntries = [CreateLocalEntry("intro")];
                    configuration.DefaultVideoId = "intro";
                }));

            Assert.Null(exception);
        }

        [Fact]
        public void AddVideoPlayerChannel_WithADuplicatePlaylistVideoId_Throws()
        {
            var services = CreateHostServices();

            Assert.Throws<ArgumentException>(() =>
                services.AddVideoPlayerChannel(configuration =>
                {
                    configuration.PlaylistPolicy = LocalPolicy();
                    configuration.PlaylistEntries = [CreateLocalEntry("intro"), CreateLocalEntry("intro")];
                }));
        }

        [Fact]
        public void AddVideoPlayerChannel_WithAPlaylistEntryViolatingThePolicy_Throws()
        {
            var services = CreateHostServices();

            // No PlaylistPolicy configured — its own default (no LocalFileRoot) approves nothing.
            Assert.Throws<VideoPlaylistValidationException>(() =>
                services.AddVideoPlayerChannel(configuration => configuration.PlaylistEntries = [CreateLocalEntry("intro")]));
        }

        [Fact]
        public void AddVideoPlayerChannel_RegisteredPlaylist_ResolvesTheConfiguredEntries()
        {
            var services = CreateHostServices();

            services.AddVideoPlayerChannel(configuration =>
            {
                configuration.PlaylistPolicy = LocalPolicy();
                configuration.PlaylistEntries = [CreateLocalEntry("intro"), CreateLocalEntry("outro", isEnabled: false)];
            });
            var serviceProvider = services.BuildServiceProvider();
            var playlist = serviceProvider.GetRequiredService<IVideoPlaylist>();

            Assert.True(playlist.TryGetEntry("intro", out var introEntry));
            Assert.True(introEntry!.IsEnabled);

            Assert.True(playlist.TryGetEntry("outro", out var outroEntry));
            Assert.False(outroEntry!.IsEnabled);

            Assert.False(playlist.TryGetEntry("unknown", out _));
        }
    }
}
