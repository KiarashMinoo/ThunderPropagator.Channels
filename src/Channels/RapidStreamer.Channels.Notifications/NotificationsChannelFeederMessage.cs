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
            get => GetValueOrDefault(DateTime.UtcNow);
            private set => SetValue(value);
        }

        public TimeSpan Time
        {
            get => GetValueOrDefault(DateTime.UtcNow.TimeOfDay);
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
            get => GetValueOrDefault(NotificationType.Text);
            private set => SetValue(value);
        }

        public NotificationPriority Priority
        {
            get => GetValueOrDefault(NotificationPriority.Normal);
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