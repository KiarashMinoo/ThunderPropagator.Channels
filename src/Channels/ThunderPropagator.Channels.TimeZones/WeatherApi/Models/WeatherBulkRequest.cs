using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ThunderPropagator.Channels.TimeZones.WeatherApi.Models
{
    public
#if !DEBUG
        sealed
#endif
        class WeatherBulkRequest
    {
        public
#if !DEBUG
        sealed
#endif
            class WeatherBulkRequestLocation
        {
            [JsonProperty("q")]
            [JsonPropertyName("q")]
            public required string Query { get; set; }

            [JsonProperty("custom_id")]
            [JsonPropertyName("custom_id")]
            public string? CustomId { get; set; }

            public override int GetHashCode() => !string.IsNullOrWhiteSpace(CustomId) ? HashCode.Combine(Query, CustomId) : Query.GetHashCode();
        }

        [JsonProperty("locations")]
        [JsonPropertyName("locations")]
        public List<WeatherBulkRequestLocation> Locations { get; set; } = [];

        public override int GetHashCode() => Locations.Aggregate(0, HashCode.Combine);
    }
}