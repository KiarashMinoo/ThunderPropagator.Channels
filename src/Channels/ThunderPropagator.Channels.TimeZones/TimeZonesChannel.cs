using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.BuildingBlocks.Application.Helpers;

namespace ThunderPropagator.Channels.TimeZones
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