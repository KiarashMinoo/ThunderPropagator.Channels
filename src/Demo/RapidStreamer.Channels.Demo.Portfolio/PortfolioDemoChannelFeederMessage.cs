using RapidStreamer.BuildingBlocks.Application;
using RapidStreamer.BuildingBlocks.Application.Enums;

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
            CastType = CastType.Unicast;
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
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public string Stock
        {
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public decimal Price
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