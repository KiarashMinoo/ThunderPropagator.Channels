using RapidStreamer.BuildingBlocks.Application;

namespace RapidStreamer.Channels.TimeZones
{
    internal
#if !DEBUG
        sealed
#endif
        class TimeZonesChannelFeederMessage : FeederMessage
    {
        public string TimeZone
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public DateTime Date
        {
            get => GetValueOrDefault(DateTime.UtcNow);
            set => SetValue(value);
        }

        public TimeSpan Time
        {
            get => GetValueOrDefault(DateTime.UtcNow.TimeOfDay);
            set => SetValue(value);
        }

        /// <summary>
        /// ZoneId/Clock
        /// </summary>
        public string WeatherKey
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public double Celsius
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public double Fahrenheit
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public string Condition
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public string ConditionIcon
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public string Target
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public DateTime TargetDate
        {
            get => GetValueOrDefault(DateTime.UtcNow);
            set => SetValue(value);
        }

        public TimeSpan TargetTime
        {
            get => GetValueOrDefault(DateTime.UtcNow.TimeOfDay);
            set => SetValue(value);
        }
    }
}