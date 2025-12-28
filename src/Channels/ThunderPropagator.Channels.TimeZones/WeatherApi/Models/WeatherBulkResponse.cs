using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ThunderPropagator.Channels.TimeZones.WeatherApi.Models
{
    public
#if !DEBUG
        sealed
#endif
        class WeatherBulkResponse
    {
        public
#if !DEBUG
        sealed
#endif
            class WeatherBulkResponseObject
        {
            public
#if !DEBUG
        sealed
#endif
                class WeatherBulkQueryResponse : WeatherResponse
            {
                [JsonProperty("custom_id")]
                [JsonPropertyName("custom_id")]
                public string? CustomId { get; set; }

                [JsonProperty("q")]
                [JsonPropertyName("q")]
                public string Query { get; set; } = null!;
            }

            [JsonProperty("query")]
            [JsonPropertyName("query")]
            public WeatherBulkQueryResponse Query { get; set; } = null!;
        }

        [JsonProperty("bulk")]
        [JsonPropertyName("bulk")]
        public List<WeatherBulkResponseObject> Bulk { get; set; } = [];
    }
}