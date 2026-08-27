using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Channels.Demo.VideoPlayer.Channel;
using ThunderPropagator.Channels.Demo.VideoPlayer.Configuration;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Extensions
{
    public static class VideoPlayerChannelExtensions
    {
        // Issue #213: registers only the channel itself for now — no decoder/session services, command
        // pipelines, or feeder-equivalent decode/pace loop exist yet; those are separate, already-scoped
        // child issues (#216-220 media source/session, #225-230 command pipelines) that this scaffold
        // exists to support.
        public static IServiceCollection AddVideoPlayerChannel(this IServiceCollection services, Action<VideoPlayerChannelConfiguration>? channelConfigurator = null)
        {
            VideoPlayerChannelConfiguration videoPlayerChannelConfiguration = new();
            channelConfigurator?.Invoke(videoPlayerChannelConfiguration);
            // Issue #234: fails host startup immediately, with a property-specific message, rather
            // than letting an out-of-range value surface later as a confusing runtime failure — mirrors
            // AddChatChannel's own ChatChannelConfiguration.Validate() call. No IVideoPlaylist exists in
            // DI yet to cross-check DefaultVideoId against (#238's own unfulfilled scope), so that one
            // specific check is skipped here — see VideoPlayerChannelConfiguration.Validate's own remarks.
            videoPlayerChannelConfiguration.Validate();

            services
                .AddSingleton(videoPlayerChannelConfiguration)
                .AddChannel<VideoPlayerChannel>();

            return services;
        }
    }
}
