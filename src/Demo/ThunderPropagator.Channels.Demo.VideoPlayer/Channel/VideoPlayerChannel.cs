using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Demo.VideoPlayer.Configuration;
using ThunderPropagator.Channels.Demo.VideoPlayer.Messages;
using ThunderPropagator.Channels.Demo.VideoPlayer.Metadata;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Channel
{
    public
#if !DEBUG
        sealed
#endif
        class VideoPlayerChannel : AbstractChannel<VideoPlayerChannelMetadata, VideoPlayerChannelConfiguration>
    {
        public VideoPlayerChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Pushes <paramref name="message"/> to every subscriber of this channel instance. Thin wrapper
        /// around the protected base broadcast primitive so pipelines (a separate class hierarchy, not
        /// derived from <see cref="AbstractChannel{TMetadata,TConfiguration}"/>) can invoke it — mirrors
        /// <c>RockPaperScissorsChannel.SendAsync</c>'s own equivalent wrapper.
        /// </summary>
        internal Task BroadcastAsync(VideoPlayerChannelFeederMessage message, CancellationToken cancellationToken = default) =>
            EmitMessageAsync(message, cancellationToken);
    }
}
