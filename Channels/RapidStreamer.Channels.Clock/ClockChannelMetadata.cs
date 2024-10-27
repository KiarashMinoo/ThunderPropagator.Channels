using RapidStreamer.Application.Channels.ChannelProgramsDescriptors;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using RapidStreamer.Application.Channels.Metadata;

namespace RapidStreamer.Channels.Clock
{
    public
#if !DEBUG
        sealed
#endif
        class ClockChannelMetadata : AbstractChannelMetadata<ClockChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors
            => new()
            {
                new SubscribingKeyChannelProgramsDescriptor(0, nameof(ClockChannelFeederMessage.Key), "the key, key must be set \"Now\" or \"UtcNow\""),
                new DateChannelProgramsDescriptor(1, nameof(ClockChannelFeederMessage.Date), "The current date"),
                new TimeChannelProgramsDescriptor(2, nameof(ClockChannelFeederMessage.Time), "The current time"),
                new DateTimeChannelProgramsDescriptor(3, nameof(ClockChannelFeederMessage.DateTime), "The current date and time")
            };

        public ClockChannelMetadata()
        {
            SetMaxFrequency(3);
        }
    }
}