using RapidStreamer.BuildingBlocks.Application;

namespace RapidStreamer.Channels.Demo.StockListBasic
{
    public
#if !DEBUG
        sealed
#endif
        class StockListBasicDemoChannelFeederMessage : FeederMessage
    {
        public string Key
        {
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public string Stock
        {
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public decimal OrderPrice
        {
            get => GetValueOrDefault(0);
            internal set => SetValue(value);
        }

        public decimal TradePrice
        {
            get => GetValueOrDefault(0);
            internal set => SetValue(value);
        }

        public decimal ReferencePrice
        {
            get => GetValueOrDefault(0);
            internal set
            {
                SetValue(value);

                LowerPrice = value - value * .05m;
                UpperPrice = value + value * .05m;
            }
        }

        public decimal LowerPrice
        {
            get => GetValueOrDefault(0);
            private set => SetValue(value);
        }

        public decimal UpperPrice
        {
            get => GetValueOrDefault(0);
            private set => SetValue(value);
        }

        public decimal LastPrice
        {
            get => GetValueOrDefault(0);
            internal set => SetValue(value);
        }

        public decimal OpeningPrice
        {
            get => GetValueOrDefault(0);
            internal set => SetValue(value);
        }

        public decimal Change
        {
            get => GetValueOrDefault(0);
            internal set => SetValue(value);
        }

        public decimal ChangePercent
        {
            get => GetValueOrDefault(0);
            internal set => SetValue(value);
        }

        public int Quantity
        {
            get => GetValueOrDefault(0);
            internal set => SetValue(value);
        }

        public TimeSpan Time
        {
            get => GetValueOrDefault(DateTime.UtcNow.TimeOfDay);
            internal set => SetValue(value);
        }
    }
}