using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.VideoPlayer
{
    public
#if !DEBUG
        sealed
#endif
        class VideoPlayerChannelConfiguration : AbstractChannelConfiguration
    {
        public VideoPlayerChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}
