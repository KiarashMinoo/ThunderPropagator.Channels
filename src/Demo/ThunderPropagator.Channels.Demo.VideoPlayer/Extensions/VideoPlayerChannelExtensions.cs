using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Configuration;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Audio;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Diagnostics;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Session;
using ThunderPropagator.Channels.Demo.VideoPlayer.Media.Video;
using ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Join;
using ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Pause;
using ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Play;
using ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.React;
using ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Seek;
using ThunderPropagator.Channels.Demo.VideoPlayer.Pipelines.Select;
using ThunderPropagator.Channels.Demo.VideoPlayer.Playlist;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Extensions
{
    /// <summary>
    /// #238's own scope, "one extension call registers all required server-side services" —
    /// <see cref="AddVideoPlayerChannel"/> is now the complete registration surface for this channel:
    /// state, the shared <see cref="VideoPlaybackSessionManager"/>, the FFmpeg-backed source/decoder
    /// (bounded by <see cref="VideoPlayerChannelConfiguration.SourceOpenTimeout"/> via
    /// <see cref="TimeoutVideoFrameSource"/>/<see cref="TimeoutAudioFrameSource"/>), the approved-video
    /// <see cref="IVideoPlaylist"/>, every <c>Video/*</c> receive pipeline, options validation, telemetry,
    /// and shutdown cleanup. #213 registered only the channel itself; #216-224 built the media pipeline
    /// this wires up; #225-231 built the command pipelines this now registers; #234 built the validated
    /// configuration this now actually maps into those lower-level pieces.
    /// </summary>
    public static class VideoPlayerChannelExtensions
    {
        public static IServiceCollection AddVideoPlayerChannel(this IServiceCollection services, Action<VideoPlayerChannelConfiguration>? channelConfigurator = null)
        {
            VideoPlayerChannelConfiguration videoPlayerChannelConfiguration = new();
            channelConfigurator?.Invoke(videoPlayerChannelConfiguration);

            // #233's own allow-list, constructed here (not lazily inside a DI factory) so an invalid
            // entry fails host startup immediately, in the same pass as every other configuration
            // problem — #234's own AC, "Invalid dimensions, quality, timeouts, encoding, or playlist
            // references fail startup clearly." Built before Validate() so DefaultVideoId can actually be
            // cross-checked against it, closing the one check that property's own remarks describe as
            // "only cross-checked... when one happens to be supplied."
            var playlist = new InMemoryVideoPlaylist(videoPlayerChannelConfiguration.PlaylistEntries, videoPlayerChannelConfiguration.PlaylistPolicy);
            videoPlayerChannelConfiguration.Validate(playlist);

            services.TryAddSingleton(videoPlayerChannelConfiguration);
            services.TryAddSingleton<IVideoPlaylist>(playlist);
            services.TryAddSingleton<VideoPlaybackTelemetry>();
            services.TryAddSingleton(serviceProvider => CreateSessionManager(videoPlayerChannelConfiguration, serviceProvider));

            services
                .AddChannel<VideoPlayerChannel>()
                .AddReceivePipeline<VideoPlayerChannel, VideoPlayerJoinReceiverPipeline>()
                .AddReceivePipeline<VideoPlayerChannel, VideoPlayerPlayReceiverPipeline>()
                .AddReceivePipeline<VideoPlayerChannel, VideoPlayerPauseReceiverPipeline>()
                .AddReceivePipeline<VideoPlayerChannel, VideoPlayerSeekReceiverPipeline>()
                .AddReceivePipeline<VideoPlayerChannel, VideoPlayerSelectReceiverPipeline>();

            // EnableReactions removes Video/React's own server runtime path entirely rather than merely
            // rejecting every reaction once registered — that property's own remarks call this out
            // explicitly as #238's job. A caller that flips EnableReactions on later needs a fresh
            // AddVideoPlayerChannel call (i.e. a host restart) to register it, consistent with every other
            // setting here only taking effect through this one registration call.
            if (videoPlayerChannelConfiguration.EnableReactions)
                services.AddReceivePipeline<VideoPlayerChannel, VideoPlayerReactReceiverPipeline>();

            return services;
        }

        /// <summary>
        /// Builds the one <see cref="VideoPlaybackSessionManager"/> this channel shares — its own
        /// <c>sessionFactory</c> maps every already-validated <see cref="VideoPlayerChannelConfiguration"/>
        /// value into the lower-level options types <see cref="VideoPlaybackSession"/>/
        /// <see cref="FfmpegVideoFrameSourceOptions"/>/<see cref="FfmpegAudioFrameSourceOptions"/> already
        /// accept — the mapping that type's own class-level remarks describe as this ticket's job.
        /// </summary>
        private static VideoPlaybackSessionManager CreateSessionManager(VideoPlayerChannelConfiguration configuration, IServiceProvider serviceProvider)
        {
            var telemetry = serviceProvider.GetRequiredService<VideoPlaybackTelemetry>();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            // No IHostApplicationLifetime means no cancelable shutdown token to link — the manager (and
            // every session it creates) simply never auto-disposes on host shutdown, mirroring
            // VideoPlaybackSessionManager's own "a non-cancelable token means disposed only when its
            // owner explicitly calls DisposeAsync" default rather than throwing for a host that hasn't
            // registered one (e.g. a minimal test host).
            var hostShutdownToken = serviceProvider.GetService<IHostApplicationLifetime>()?.ApplicationStopping ?? default;

            var sessionOptions = new VideoPlaybackSessionOptions
            {
                DecodeBufferCapacity = configuration.DecodeBufferCapacity,
                SubscriberQueueCapacity = configuration.SubscriberQueueCapacity,
                PlaybackRate = configuration.PlaybackRate,
                Encoding = configuration.Encoding,
                Quality = configuration.Quality,
                PollInterval = configuration.PollInterval,
                EnableAudio = configuration.EnableAudio,
                AudioDecodeBufferCapacity = configuration.AudioDecodeBufferCapacity,
                AudioSubscriberQueueCapacity = configuration.AudioSubscriberQueueCapacity,
                AudioBitRate = configuration.AudioBitRate,
                AudioEncoding = configuration.AudioEncoding,
                // Mirrors EnableReactions' own effect on pipeline registration above: an empty allowed
                // set makes every reaction rejected by ReactionAggregator itself even if a caller
                // somehow still reached it, a defense-in-depth backstop under the pipeline-level cut.
                AllowedReactions = configuration.EnableReactions ? configuration.AllowedReactions : new HashSet<string>(),
                ReactionWindow = configuration.ReactionWindow,
                MaxReactionsPerViewerPerWindow = configuration.MaxReactionsPerViewerPerWindow
            };

            return new VideoPlaybackSessionManager(
                sessionId => new VideoPlaybackSession(
                    // SessionId, when configured, replaces every session's own runtime-generated
                    // ChannelKey-derived id — see that property's own remarks. The manager's own
                    // dictionary is still keyed by the real ChannelKey (sessionId here); only the
                    // constructed session's own SessionId — what reaches telemetry tags and the wire —
                    // changes.
                    configuration.SessionId ?? sessionId,
                    () => CreateVideoSource(configuration),
                    new SystemMonotonicClock(),
                    sessionOptions,
                    hostShutdownToken: hostShutdownToken,
                    audioSourceFactory: configuration.EnableAudio ? () => CreateAudioSource(configuration) : null,
                    telemetry: telemetry,
                    logger: loggerFactory?.CreateLogger<VideoPlaybackSession>()),
                hostShutdownToken);
        }

        private static IVideoFrameSource CreateVideoSource(VideoPlayerChannelConfiguration configuration)
        {
            IVideoFrameSource source = new FfmpegVideoFrameSource(new FfmpegVideoFrameSourceOptions
            {
                MaxWidth = configuration.MaxWidth,
                MaxHeight = configuration.MaxHeight
            });

            return configuration.SourceOpenTimeout is { } timeout ? new TimeoutVideoFrameSource(source, timeout) : source;
        }

        private static IAudioFrameSource CreateAudioSource(VideoPlayerChannelConfiguration configuration)
        {
            IAudioFrameSource source = new FfmpegAudioFrameSource(new FfmpegAudioFrameSourceOptions());

            return configuration.SourceOpenTimeout is { } timeout ? new TimeoutAudioFrameSource(source, timeout) : source;
        }
    }
}
