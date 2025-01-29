using RapidStreamer.BuildingBlocks.Application;

namespace RapidStreamer.Channels.Notifications
{
    public
#if !DEBUG
        sealed
#endif
        class NotificationsChannelFeederMessage : FeederMessage
    {
        public NotificationsChannelFeederMessage()
        {
        }

        internal NotificationsChannelFeederMessage(IDictionary<string, object?> feederMessage) : this()
        {
            foreach (var item in feederMessage)
            {
                SetValue(item.Value, item.Key);
            }
        }

        public string? UserId
        {
            get => GetValueOrNull<string>();
            set => SetValue(value);
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

        public string Id
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public string Origin
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public NotificationType Type
        {
            get => GetValue<NotificationType>();
            private set => SetValue(value);
        }

        public NotificationPriority Priority
        {
            get => GetValue<NotificationPriority>();
            private set => SetValue(value);
        }

        public string Icon
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public string Subject
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public string Body
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public string EllipsisBody
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public int Seen
        {
            get => GetValueOrDefault<int>(0);
            set => SetValue(value);
        }

        public string Metadata
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }
    }
}