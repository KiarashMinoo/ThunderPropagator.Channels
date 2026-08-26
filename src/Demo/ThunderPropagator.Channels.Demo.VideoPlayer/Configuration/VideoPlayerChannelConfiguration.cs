using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.VideoPlayer.Configuration
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
