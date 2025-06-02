using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.ResourceMonitoring
{
    public
#if !DEBUG
        sealed
#endif
        class ResourceMonitoringChannelConfiguration : AbstractChannelConfiguration
    {
        public ResourceMonitoringChannelConfiguration()
        {
            IsEnabled = true;
        }

        public ResourceMonitoringChannelFeederConfiguration FeederConfiguration { get; set; } = new();
    }
}