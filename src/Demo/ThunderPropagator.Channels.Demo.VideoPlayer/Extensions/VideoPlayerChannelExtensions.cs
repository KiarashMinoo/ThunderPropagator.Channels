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

            services
                .AddSingleton(videoPlayerChannelConfiguration)
                .AddChannel<VideoPlayerChannel>();

            return services;
        }
    }
}
