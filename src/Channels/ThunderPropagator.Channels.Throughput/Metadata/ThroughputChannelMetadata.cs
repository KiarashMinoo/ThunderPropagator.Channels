using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.Channels.Throughput.Channel;
using ThunderPropagator.Channels.Throughput.Messages;

namespace ThunderPropagator.Channels.Throughput.Metadata
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