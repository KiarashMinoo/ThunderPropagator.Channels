using RapidStreamer.BuildingBlocks.Application;

namespace RapidStreamer.Channels.Clock
{
    internal
#if !DEBUG
        sealed
#endif
        class ClockChannelFeederMessage : FeederMessage
    {
        public ClockChannelFeederMessage()
        {
        }

        internal ClockChannelFeederMessage(string key, DateTime dateTime) : this()
        {
            Key = key;

            Date = dateTime.Date;
            Time = dateTime.TimeOfDay;
            DateTime = dateTime;
        }

        public string Key
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public DateTime Date
        {
            get => GetValue<DateTime>();
            private set => SetValue(value);
        }

        public TimeSpan Time
        {
            get => GetValue<TimeSpan>();
            private set => SetValue(value);
        }

        public DateTime DateTime
        {
            get => GetValue<DateTime>();
            private set => SetValue(value);
        }
    }
}