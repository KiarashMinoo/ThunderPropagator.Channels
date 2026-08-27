using ThunderPropagator.Application.Feeders;
using ThunderPropagator.BuildingBlocks.Application.Enums;

namespace ThunderPropagator.Channels.TimeZones.Feeders;

public
#if !DEBUG
    sealed
#endif
    class TimeZonesChannelFeederConfiguration : AbstractFeederConfiguration
{
    public string? Proxy
    {
        get => Get<string>();
        set => Set(value);
    }

    public string WeatherApiUrl
    {
        get => Get("http://api.weatherapi.com/v1");
        set => Set(value);
    }

    /// <summary>
    /// The WeatherAPI.com API key <see cref="WeatherApi.WeatherApiService"/> authenticates with. No
    /// default — must be supplied via configuration (environment variable, user secrets, or a secrets
    /// manager) before the TimeZones feeder is enabled; <see cref="Extensions.TimeZonesChannelExtensions.AddTimeZonesChannel"/>
    /// throws at startup if it is missing while <see cref="AbstractFeederConfiguration.IsEnabled"/> is
    /// <see langword="true"/>. A real key must never be hardcoded here or anywhere else in source — see
    /// https://github.com/KiarashMinoo/ThunderPropagator.Channels/issues/10.
    /// </summary>
    public string? WeatherApiKey
    {
        get => Get<string>();
        set => Set(value);
    }

    public string RedisCacheConnectionString
    {
        get => Get(string.Empty);
        set => Set(value);
    }

    public string SnapshotConnectionString
    {
        get => Get(string.Empty);
        set => Set(value);
    }

    public RecoveryStorage SnapshotRecoveryStorage
    {
        get => Get(RecoveryStorage.Postgresql);
        set => Set(value);
    }

    public int SnapshotTtlHours
    {
        get => Get(24);
        set => Set(value);
    }

    public TimeZonesChannelFeederConfiguration()
    {
        IsEnabled = false;
    }

    internal void Bind(TimeZonesChannelFeederConfiguration timeZonesChannelFeederConfiguration) => base.Bind(timeZonesChannelFeederConfiguration);
}