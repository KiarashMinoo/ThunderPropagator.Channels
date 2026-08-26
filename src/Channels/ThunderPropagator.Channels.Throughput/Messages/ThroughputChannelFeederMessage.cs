using ThunderPropagator.BuildingBlocks.Application;

namespace ThunderPropagator.Channels.Throughput.Messages
{
    internal
#if !DEBUG
        sealed
#endif
        class ThroughputChannelFeederMessage : FeederMessage
    {
        public string Key
        {
            get => GetValueOrDefault(string.Empty);
            private set => SetValue(value);
        }

        public long UpStreamHandled
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public long DownStreamHandled
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public double DownStreamSize
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public long DownStreamDuration
        {
            get => GetValueOrDefault(0);
            set => SetValue(value);
        }

        public ThroughputChannelFeederMessage()
        {
            Key = nameof(Throughput);
        }
    }
}