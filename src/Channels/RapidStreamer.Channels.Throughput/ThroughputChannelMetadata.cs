using RapidStreamer.Application.Channels.ChannelProgramsDescriptors;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using RapidStreamer.Application.Channels.Metadata;

namespace RapidStreamer.Channels.Throughput
{
    public
#if !DEBUG
        sealed
#endif
        class ThroughputChannelMetadata : AbstractChannelMetadata<ThroughputChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors
            => new()
            {
                new SubscribingKeyChannelProgramsDescriptor(0, nameof(ThroughputChannelFeederMessage.Key), "the key, key must be set \"Throughput\""),
                new NumberChannelProgramsDescriptor(1, nameof(ThroughputChannelFeederMessage.UpStreamHandled), "Gets the count of the upstream handled."),
                new NumberChannelProgramsDescriptor(2, nameof(ThroughputChannelFeederMessage.DownStreamHandled), "Gets the count of the downstream handled."),
                new NumberChannelProgramsDescriptor(3, nameof(ThroughputChannelFeederMessage.DownStreamSize), "ets the size of the downstream handled in bytes."),
                new DecimalChannelProgramsDescriptor(4, nameof(ThroughputChannelFeederMessage.DownStreamDuration), "Gets the average milliseconds of handled downstream."),
            };
    }
}