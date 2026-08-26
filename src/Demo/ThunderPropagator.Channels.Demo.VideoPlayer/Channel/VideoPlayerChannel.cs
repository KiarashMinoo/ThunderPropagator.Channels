using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Demo.VideoPlayer.Configuration;
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
    }
}
