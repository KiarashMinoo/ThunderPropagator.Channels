using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Application.Channels;

namespace RapidStreamer.Channels.TimeZones
{
    public
#if !DEBUG
        sealed
#endif
        class TimeZonesChannel : AbstractChannel<TimeZonesChannelMetadata>
    {
        public TimeZonesChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            var timeZonesChannelFeederConfiguration = serviceProvider.GetRequiredService<TimeZonesChannelFeederConfiguration>();

            Metadata.SetChannelSnapshot(
                timeZonesChannelFeederConfiguration.SnapshotConnectionString,
                timeZonesChannelFeederConfiguration.SnapshotRecoveryStorage,
                timeZonesChannelFeederConfiguration.SnapshotTtlHours);
        }
    }
}