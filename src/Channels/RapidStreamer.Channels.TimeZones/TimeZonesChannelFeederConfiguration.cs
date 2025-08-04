using RapidStreamer.Application.Feeders;
using RapidStreamer.BuildingBlocks.Application.Enums;

namespace RapidStreamer.Channels.TimeZones;

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

    public string WeatherApiKey
    {
        get => Get("24660490d3384f0abb2113538241408");
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