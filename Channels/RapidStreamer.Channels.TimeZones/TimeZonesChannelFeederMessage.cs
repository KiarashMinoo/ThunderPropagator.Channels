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
            get => GetValue<DateTime>();
            set => SetValue(value);
        }

        public TimeSpan Time
        {
            get => GetValue<TimeSpan>();
            set => SetValue(value);
        }

        /// <summary>
        /// ZoneId/Clock
        /// </summary>
        public string WeatherKey
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public double Celsius
        {
            get => GetValue<int>();
            set => SetValue(value);
        }

        public double Fahrenheit
        {
            get => GetValue<int>();
            set => SetValue(value);
        }

        public string Condition
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string ConditionIcon
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string Target
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public DateTime TargetDate
        {
            get => GetValue<DateTime>();
            set => SetValue(value);
        }

        public TimeSpan TargetTime
        {
            get => GetValue<TimeSpan>();
            set => SetValue(value);
        }
    }
}