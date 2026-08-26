using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.Channels.Demo.Portfolio.Channel;
using ThunderPropagator.Channels.Demo.Portfolio.Messages;

namespace ThunderPropagator.Channels.Demo.Portfolio.Metadata
{
    public
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelMetadata : AbstractChannelMetadata<PortfolioDemoChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new()
        {
            new SubscribingKeyChannelProgramsDescriptor(0, nameof(PortfolioDemoChannelFeederMessage.Key)).SetTable(PortfolioDemoChannel.PortfolioDemo),
            new SubscribingKeyChannelProgramsDescriptor(1, nameof(PortfolioDemoChannelFeederMessage.Stock)).SetTable(PortfolioDemoChannel.PortfolioDemoItems),
            new CurrencyChannelProgramsDescriptor(2, nameof(PortfolioDemoChannelFeederMessage.Price), "The price").SetTable(PortfolioDemoChannel.PortfolioDemoItems),
            new NumberChannelProgramsDescriptor(3, nameof(PortfolioDemoChannelFeederMessage.Quantity), "The quantity").SetTable(PortfolioDemoChannel.PortfolioDemoItems),
            new TimeChannelProgramsDescriptor(4, nameof(PortfolioDemoChannelFeederMessage.Time), "The last trade time").SetTable(PortfolioDemoChannel.PortfolioDemoItems)
        };
    }
}