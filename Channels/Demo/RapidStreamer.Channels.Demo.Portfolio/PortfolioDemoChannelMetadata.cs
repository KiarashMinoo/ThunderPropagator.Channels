using RapidStreamer.Application.Channels.ChannelProgramsDescriptors;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using RapidStreamer.Application.Channels.Metadata;

namespace RapidStreamer.Channels.Demo.Portfolio
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