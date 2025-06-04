using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.ResourceMonitoring
{
    public
#if !DEBUG
        sealed
#endif
        class ResourceMonitoringChannelConfiguration : AbstractChannelConfiguration
    {
        public ResourceMonitoringChannelFeederConfiguration FeederConfiguration { get; set; } = new();

        public ResourceMonitoringChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}