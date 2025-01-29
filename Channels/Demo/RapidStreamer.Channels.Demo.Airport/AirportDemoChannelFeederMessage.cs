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
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public string Destination
        {
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public TimeSpan Departure
        {
            get => GetValueOrDefault(DateTime.UtcNow.TimeOfDay);
            internal set => SetValue(value);
        }

        public string Flight
        {
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public string Airline
        {
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }

        public int Terminal
        {
            get => GetValueOrDefault(0);
            internal set => SetValue(value);
        }

        public string Status
        {
            get => GetValueOrDefault(string.Empty);
            internal set => SetValue(value);
        }
    }
}