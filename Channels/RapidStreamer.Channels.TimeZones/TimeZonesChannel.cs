using Microsoft.Extensions.DependencyInjection;
using RapidStreamer.Application.Channels;
using RapidStreamer.BuildingBlocks.Application.Helpers;

namespace RapidStreamer.Channels.TimeZones
{
    public
#if !DEBUG
        sealed
#endif
        class TimeZonesChannel : AbstractChannel<TimeZonesChannelMetadata, TimeZonesChannelConfiguration>
    {
        public TimeZonesChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            var timeZonesChannelFeederConfiguration = serviceProvider.GetRequiredService<TimeZonesChannelFeederConfiguration>();

            Metadata.SetChannelSnapshot(
                ConnectionStringHelper.EnrichConnectionString(timeZonesChannelFeederConfiguration.SnapshotConnectionString),
                timeZonesChannelFeederConfiguration.SnapshotRecoveryStorage,
                timeZonesChannelFeederConfiguration.SnapshotTtlHours);
        }
    }
}