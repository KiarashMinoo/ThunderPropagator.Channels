using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RapidStreamer.Channels.TimeZones.WeatherApi.Models
{
    public
#if !DEBUG
        sealed
#endif
        class WeatherException
    {
        public class WeatherExceptionError
        {
            [JsonProperty("code")]
            [JsonPropertyName("code")]
            public int Code { get; set; }

            [JsonProperty("message")]
            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }

        [JsonProperty("error")]
        [JsonPropertyName("error")]
        public WeatherExceptionError? Error { get; set; }
    }
}