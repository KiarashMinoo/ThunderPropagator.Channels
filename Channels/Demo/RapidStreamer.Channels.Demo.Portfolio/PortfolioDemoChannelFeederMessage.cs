using RapidStreamer.BuildingBlocks.Application;

namespace RapidStreamer.Channels.Demo.Portfolio
{
    public
#if !DEBUG
        sealed
#endif
        class PortfolioDemoChannelFeederMessage : FeederMessage
    {
        public PortfolioDemoChannelFeederMessage()
        {
        }

        internal PortfolioDemoChannelFeederMessage(IReadOnlyDictionary<string, object?> feederMessage) : this()
        {
            foreach (var item in feederMessage)
            {
                SetValue(item.Value, item.Key);
            }
        }

        public string Key
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }

        public string Stock
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }

        public decimal Price
        {
            get => GetValue<decimal>();
            internal set => SetValue(value);
        }

        public int Quantity
        {
            get => GetValue<int>();
            internal set => SetValue(value);
        }

        public TimeSpan Time
        {
            get => GetValue<TimeSpan>();
            internal set => SetValue(value);
        }
    }
}