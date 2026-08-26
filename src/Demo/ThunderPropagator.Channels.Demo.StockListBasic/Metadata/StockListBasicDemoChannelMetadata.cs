using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors;
using ThunderPropagator.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using ThunderPropagator.Application.Channels.Metadata;
using ThunderPropagator.Channels.Demo.StockListBasic.Channel;
using ThunderPropagator.Channels.Demo.StockListBasic.Messages;

namespace ThunderPropagator.Channels.Demo.StockListBasic.Metadata
{
    public
#if !DEBUG
        sealed
#endif
        class StockListBasicDemoChannelMetadata : AbstractChannelMetadata<StockListBasicDemoChannel>
    {
        public const string StockListBasicDemo = nameof(StockListBasicDemo);
        public const string StockListBasicDemoItems = nameof(StockListBasicDemoItems);

        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors => new()
        {
            new SubscribingKeyChannelProgramsDescriptor(0, nameof(StockListBasicDemoChannelFeederMessage.Key)).SetTable(StockListBasicDemo),
            new SubscribingKeyChannelProgramsDescriptor(1, nameof(StockListBasicDemoChannelFeederMessage.Stock)).SetTable(StockListBasicDemoItems),
            new CurrencyChannelProgramsDescriptor(3, nameof(StockListBasicDemoChannelFeederMessage.OrderPrice)).SetTable(StockListBasicDemoItems),
            new CurrencyChannelProgramsDescriptor(4, nameof(StockListBasicDemoChannelFeederMessage.TradePrice)).SetTable(StockListBasicDemoItems),
            new CurrencyChannelProgramsDescriptor(5, nameof(StockListBasicDemoChannelFeederMessage.ReferencePrice)).SetTable(StockListBasicDemoItems),
            new CurrencyChannelProgramsDescriptor(6, nameof(StockListBasicDemoChannelFeederMessage.LowerPrice)).SetTable(StockListBasicDemoItems),
            new CurrencyChannelProgramsDescriptor(7, nameof(StockListBasicDemoChannelFeederMessage.UpperPrice)).SetTable(StockListBasicDemoItems),
            new CurrencyChannelProgramsDescriptor(8, nameof(StockListBasicDemoChannelFeederMessage.LastPrice)).SetTable(StockListBasicDemoItems),
            new CurrencyChannelProgramsDescriptor(9, nameof(StockListBasicDemoChannelFeederMessage.OpeningPrice)).SetTable(StockListBasicDemoItems),
            new CurrencyChannelProgramsDescriptor(10, nameof(StockListBasicDemoChannelFeederMessage.Change)).SetTable(StockListBasicDemoItems),
            new PercentChannelProgramsDescriptor(11, nameof(StockListBasicDemoChannelFeederMessage.ChangePercent)).SetTable(StockListBasicDemoItems),
            new NumberChannelProgramsDescriptor(12, nameof(StockListBasicDemoChannelFeederMessage.Quantity)).SetTable(StockListBasicDemoItems),
            new TimeChannelProgramsDescriptor(13, nameof(StockListBasicDemoChannelFeederMessage.Time)).SetTable(StockListBasicDemoItems),
        };
    }
}