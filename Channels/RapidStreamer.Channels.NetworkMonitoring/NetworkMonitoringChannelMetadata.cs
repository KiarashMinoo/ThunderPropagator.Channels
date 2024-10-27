using RapidStreamer.Application.Channels.ChannelProgramsDescriptors;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using RapidStreamer.Application.Channels.Metadata;

namespace RapidStreamer.Channels.NetworkMonitoring
{
    public
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannelMetadata : AbstractChannelMetadata<NetworkMonitoringChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors
            => new()
            {
                new SubscribingKeyChannelProgramsDescriptor(0,
                    nameof(NetworkMonitoringChannelFeederMessage.Key), "the key, key must be set \"NetworkMonitoring\"").SetTable(nameof(NetworkMonitoring)),
                new SubscribingKeyChannelProgramsDescriptor(1,
                    nameof(NetworkMonitoringChannelFeederMessage.DateTime), "the date and time").SetTable($"{nameof(NetworkMonitoring)}Row"),
                new NumberChannelProgramsDescriptor(2,
                    nameof(NetworkMonitoringChannelFeederMessage.BytesReceived), "Gets the bytes received over the tcp and udp.").SetTable($"{nameof(NetworkMonitoring)}Row"),
                new NumberChannelProgramsDescriptor(3,
                    nameof(NetworkMonitoringChannelFeederMessage.BytesSent), "Gets the bytes sent over the tcp and udp.").SetTable($"{nameof(NetworkMonitoring)}Row")
            };
    }
}