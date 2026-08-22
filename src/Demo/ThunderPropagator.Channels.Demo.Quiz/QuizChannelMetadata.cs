using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.Metadata;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    // Issue #183: pure scaffold — no descriptors yet. #185 defines the real descriptor set once
    // QuizChannelFeederMessage's fields (#186) exist to describe.
    public
#if !DEBUG
        sealed
#endif
        class QuizChannelMetadata : AbstractChannelMetadata<QuizChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new();
    }
}
