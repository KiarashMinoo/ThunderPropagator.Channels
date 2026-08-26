using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.BuildingBlocks.Application.Helpers;
using ThunderPropagator.Channels.TimeZones.Configuration;
using ThunderPropagator.Channels.TimeZones.Feeders;
using ThunderPropagator.Channels.TimeZones.Metadata;

namespace ThunderPropagator.Channels.TimeZones.Channel
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