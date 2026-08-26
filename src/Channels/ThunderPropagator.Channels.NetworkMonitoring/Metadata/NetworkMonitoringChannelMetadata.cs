using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.Channels.NetworkMonitoring.Channel;
using ThunderPropagator.Channels.NetworkMonitoring.Messages;

namespace ThunderPropagator.Channels.NetworkMonitoring.Metadata
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