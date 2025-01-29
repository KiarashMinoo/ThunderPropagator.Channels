using RapidStreamer.BuildingBlocks.Application;

namespace RapidStreamer.Channels.Demo.Airport
{
    public
#if !DEBUG
        sealed
#endif
        class AirportDemoChannelFeederMessage : FeederMessage
    {
        public string Key
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }

        public string Destination
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }

        public TimeSpan Departure
        {
            get => GetValue<TimeSpan>();
            internal set => SetValue(value);
        }

        public string Flight
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }

        public string Airline
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }

        public int Terminal
        {
            get => GetValue<int>();
            internal set => SetValue(value);
        }

        public string Status
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }
    }
}