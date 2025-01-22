using RapidStreamer.BuildingBlocks.Application;

namespace RapidStreamer.Channels.Demo.StockListBasic
{
    public
#if !DEBUG
        sealed
#endif
        class StockListBasicDemoChannelFeederMessage : FeederMessage
    {
        internal StockListBasicDemoChannelFeederMessage()
        {
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

        public decimal OrderPrice
        {
            get => GetValue<decimal>();
            internal set => SetValue(value);
        }

        public decimal TradePrice
        {
            get => GetValue<decimal>();
            internal set => SetValue(value);
        }

        public decimal ReferencePrice
        {
            get => GetValue<decimal>();
            internal set
            {
                SetValue(value);

                LowerPrice = value - value * .05m;
                UpperPrice = value + value * .05m;
            }
        }

        public decimal LowerPrice
        {
            get => GetValue<decimal>();
            private set => SetValue(value);
        }

        public decimal UpperPrice
        {
            get => GetValue<decimal>();
            private set => SetValue(value);
        }

        public decimal LastPrice
        {
            get => GetValue<decimal>();
            internal set => SetValue(value);
        }

        public decimal OpeningPrice
        {
            get => GetValue<decimal>();
            internal set => SetValue(value);
        }

        public decimal Change
        {
            get => GetValue<decimal>();
            internal set => SetValue(value);
        }

        public decimal ChangePercent
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