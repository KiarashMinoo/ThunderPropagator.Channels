using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.BuildingBlocks.Application.Enums;

namespace ThunderPropagator.Channels.Demo.Airport
{
    public
#if !DEBUG
        sealed
#endif
        class AirportDemoChannelMetadata : AbstractChannelMetadata<AirportDemoChannel>
    {
        public const string AirportDemo = nameof(AirportDemo);
        public const string AirportDemoItems = nameof(AirportDemoItems);

        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new()
        {
            new SubscribingKeyChannelProgramsDescriptor(0, nameof(AirportDemoChannelFeederMessage.Key)).SetTable(AirportDemo),
            new SubscribingKeyChannelProgramsDescriptor(1, nameof(AirportDemoChannelFeederMessage.Destination)).SetTable(AirportDemoItems),
            new TimeChannelProgramsDescriptor(2, nameof(AirportDemoChannelFeederMessage.Departure)).SetTable(AirportDemoItems),
            new ChannelProgramsDescriptor(3, nameof(AirportDemoChannelFeederMessage.Flight), DataType.String).SetTable(AirportDemoItems),
            new ChannelProgramsDescriptor(4, nameof(AirportDemoChannelFeederMessage.Airline), DataType.String).SetTable(AirportDemoItems),
            new NumberChannelProgramsDescriptor(5, nameof(AirportDemoChannelFeederMessage.Terminal)).SetTable(AirportDemoItems),
            new EnumChannelProgramsDescriptor<Statuses>(6, nameof(AirportDemoChannelFeederMessage.Status)).SetTable(AirportDemoItems),
        };
    }
}