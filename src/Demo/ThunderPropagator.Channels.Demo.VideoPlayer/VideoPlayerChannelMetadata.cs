using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.Metadata;

namespace ThunderPropagator.Channels.Demo.VideoPlayer
{
    // Issue #213: pure scaffold — no descriptors yet. #215 defines the real descriptor set once
    // VideoPlayerChannelFeederMessage's fields exist to describe.
    public
#if !DEBUG
        sealed
#endif
        class VideoPlayerChannelMetadata : AbstractChannelMetadata<VideoPlayerChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new();
    }
}
