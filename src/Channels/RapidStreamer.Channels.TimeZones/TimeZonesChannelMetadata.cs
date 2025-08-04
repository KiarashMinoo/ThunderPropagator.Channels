using RapidStreamer.Application.Channels.ChannelProgramsDescriptors;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using RapidStreamer.Application.Channels.Metadata;
using RapidStreamer.BuildingBlocks.Application.Enums;

namespace RapidStreamer.Channels.TimeZones
{
    public
#if !DEBUG
        sealed
#endif
        class TimeZonesChannelMetadata : AbstractChannelMetadata<TimeZonesChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors
            => new()
            {
                new SubscribingKeyChannelProgramsDescriptor(0, nameof(TimeZonesChannelFeederMessage.TimeZone), "the source timezone.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.TimeZone)),
                new DateChannelProgramsDescriptor(1, nameof(TimeZonesChannelFeederMessage.Date), "Gets the source date.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.TimeZone)),
                new TimeChannelProgramsDescriptor(2, nameof(TimeZonesChannelFeederMessage.Time), "Gets the source time.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.TimeZone)),
                new SubscribingKeyChannelProgramsDescriptor(4, nameof(TimeZonesChannelFeederMessage.WeatherKey), "the weather key.")
                    .SetTable(nameof(WeatherApi)),
                new DecimalChannelProgramsDescriptor(5, nameof(TimeZonesChannelFeederMessage.Celsius), "Gets the temperature in celsius.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.Target)),
                new DecimalChannelProgramsDescriptor(6, nameof(TimeZonesChannelFeederMessage.Fahrenheit), "Gets the temperature in fahrenheit.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.Target)),
                new NumberChannelProgramsDescriptor(7, nameof(TimeZonesChannelFeederMessage.Condition), "Gets the condition.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.Target)),
                new NumberChannelProgramsDescriptor(8, nameof(TimeZonesChannelFeederMessage.ConditionIcon), "Gets the condition icon.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.Target)),
                new SubscribingKeyChannelProgramsDescriptor(9, nameof(TimeZonesChannelFeederMessage.Target), "the source timezone.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.Target)),
                new DateChannelProgramsDescriptor(10, nameof(TimeZonesChannelFeederMessage.TargetDate), "Gets the source date.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.Target)),
                new TimeChannelProgramsDescriptor(11, nameof(TimeZonesChannelFeederMessage.TargetTime), "Gets the source time.")
                    .SetTable(nameof(TimeZonesChannelFeederMessage.Target)),
            };

        internal void SetChannelSnapshot(string connectionString, RecoveryStorage recoveryStorage, int ttlHours)
            => SetChannelSnapshot(true,
                ttl: TimeSpan.FromHours(ttlHours),
                isTimeSeries: true,
                enableHibernation: true,
                recoveryStorage: recoveryStorage,
                connectionString: connectionString);
    }
}