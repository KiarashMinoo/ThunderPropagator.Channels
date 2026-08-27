using ThunderPropagator.Channels.TimeZones.Extensions;

namespace ThunderPropagator.Channels.TimeZones.Configuration
{
    /// <summary>
    /// Thrown by <see cref="TimeZonesChannelExtensions.AddTimeZonesChannel"/> when a required setting is
    /// missing once the consumer's own <c>channelConfigurator</c> callback has finished running — #10's
    /// own scope: a hardcoded WeatherAPI key default previously shipped in source (and so in every
    /// compiled binary/NuGet package built from this repo) instead of failing startup when one was never
    /// supplied.
    /// </summary>
    public sealed class TimeZonesChannelConfigurationValidationException(string propertyName, string rule) : Exception($"{propertyName} {rule}")
    {
        public string PropertyName { get; } = propertyName;
    }
}
