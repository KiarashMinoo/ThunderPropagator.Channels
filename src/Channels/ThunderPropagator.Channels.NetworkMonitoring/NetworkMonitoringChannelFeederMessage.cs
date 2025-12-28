using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.NetworkMonitoring
{
    internal
#if !DEBUG
        sealed
#endif
        class NetworkMonitoringChannelFeederMessage : FeederMessage
    {
        public string Key
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public string DateTime
        {
            get => GetValueOrDefault(string.Empty);
            set => SetValue(value);
        }

        public long BytesReceived
        {
            get => GetValueOrDefault(0L);
            set => SetValue(value);
        }

        public long BytesSent
        {
            get => GetValueOrDefault(0L);
            set => SetValue(value);
        }

        public NetworkMonitoringChannelFeederMessage()
        {
            Key = nameof(NetworkMonitoring);
        }
    }
}