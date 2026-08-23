using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.VideoPlayer
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
