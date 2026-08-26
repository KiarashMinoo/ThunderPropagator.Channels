using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.Channels.Clock.Channel;
using ThunderPropagator.Channels.Clock.Messages;

namespace ThunderPropagator.Channels.Clock.Metadata
{
    public
#if !DEBUG
        sealed
#endif
        class ClockChannelMetadata : AbstractChannelMetadata<ClockChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new()
        {
            new SubscribingKeyChannelProgramsDescriptor(0, nameof(ClockChannelFeederMessage.Key), "the key, key must be set \"Now\" or \"UtcNow\""),
            new DateChannelProgramsDescriptor(1, nameof(ClockChannelFeederMessage.Date), "The current date"),
            new TimeChannelProgramsDescriptor(2, nameof(ClockChannelFeederMessage.Time), "The current time"),
            new DateTimeChannelProgramsDescriptor(3, nameof(ClockChannelFeederMessage.DateTime), "The current date and time")
        };
    }
}