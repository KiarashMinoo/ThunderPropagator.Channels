using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Clock
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
            get => GetValueOrDefault(DateTime.UtcNow);
            private set => SetValue(value);
        }

        public TimeSpan Time
        {
            get => GetValueOrDefault(DateTime.UtcNow.TimeOfDay);
            private set => SetValue(value);
        }

        public DateTime DateTime
        {
            get => GetValueOrDefault(DateTime.UtcNow);
            private set => SetValue(value);
        }
    }
}