using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RapidStreamer.Channels.TimeZones.WeatherApi.Models
{
    public class WeatherResponse
    {
        public
#if !DEBUG
        sealed
#endif
            class WeatherResponseLocation
        {
            /// <summary>
            /// Location name
            /// </summary>
            [JsonProperty("name")]
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            /// <summary>
            /// Region or state of the location, if availa
            /// </summary>
            [JsonProperty("region")]
            [JsonPropertyName("region")]
            public string? Region { get; set; }

            /// <summary>
            /// Location country
            /// </summary>
            [JsonProperty("country")]
            [JsonPropertyName("country")]
            public string? Country { get; set; }

            /// <summary>
            /// Latitude in decimal degree
            /// </summary>
            [JsonProperty("lat")]
            [JsonPropertyName("lat")]
            public double? Lat { get; set; }

            /// <summary>
            /// Longitude in decimal degree
            /// </summary>
            [JsonProperty("lon")]
            [JsonPropertyName("lon")]
            public double? Lon { get; set; }

            /// <summary>
            /// Time zone name
            /// </summary>
            [JsonProperty("tz_id")]
            [JsonPropertyName("tz_id")]
            public string? TzId { get; set; }

            /// <summary>
            /// Local date and time in unix time
            /// </summary>
            [JsonProperty("localtime_epoch")]
            [JsonPropertyName("localtime_epoch")]
            public double? LocaltimeEpoch { get; set; }

            /// <summary>
            /// Local date and time
            /// </summary>
            [JsonProperty("localtime")]
            [JsonPropertyName("localtime")]
            public string? Localtime { get; set; }
        }

        public
#if !DEBUG
        sealed
#endif
            class WeatherResponseCurrent
        {
            public
#if !DEBUG
        sealed
#endif
                class WeatherResponseCurrentCondition
            {
                /// <summary>
                /// Weather condition text
                /// </summary>
                [JsonProperty("text")]
                [JsonPropertyName("text")]
                public string? Text { get; set; }

                /// <summary>
                /// Weather condition icon
                /// </summary>
                [JsonProperty("icon")]
                [JsonPropertyName("icon")]
                public string? Icon { get; set; }

                /// <summary>
                /// Weather condition code
                /// </summary>
                [JsonProperty("code")]
                [JsonPropertyName("code")]
                public double? Code { get; set; }
            }

            public
#if !DEBUG
        sealed
#endif
                class WeatherResponseCurrentAirQuality
            {
                /// <summary>
                /// Carbon Monoxide (μg/m3)
                /// </summary>
                [JsonProperty("co")]
                [JsonPropertyName("co")]
                public double? Co { get; set; }

                /// <summary>
                /// Nitrogen dioxide (μg/m3)
                /// </summary>
                [JsonProperty("no2")]
                [JsonPropertyName("no2")]
                public double? No2 { get; set; }

                /// <summary>
                /// Ozone (μg/m3)
                /// </summary>
                [JsonProperty("o3")]
                [JsonPropertyName("o3")]
                public double? O3 { get; set; }

                /// <summary>
                /// Sulphur dioxide (μg/m3)
                /// </summary>
                [JsonProperty("so2")]
                [JsonPropertyName("so2")]
                public double? So2 { get; set; }

                /// <summary>
                /// PM2.5 (μg/m3)
                /// </summary>
                [JsonProperty("pm2_5")]
                [JsonPropertyName("pm2_5")]
                public double? Pm25 { get; set; }

                /// <summary>
                /// PM10 (μg/m3)
                /// </summary>
                [JsonProperty("pm10")]
                [JsonPropertyName("pm10")]
                public double? Pm10 { get; set; }

                /// <summary>
                /// US - EPA standard.
                /// <list type="bullet">
                /// <item><description>1 means Good</description></item>
                /// <item><description>2 means Moderate</description></item>
                /// <item><description>3 means Unhealthy for sensitive group</description></item>
                /// <item><description>4 means Unhealthy</description></item>
                /// <item><description>5 means Very Unhealthy</description></item>
                /// <item><description>6 means Hazardous</description></item>
                /// </list>
                /// </summary>
                [JsonProperty("us-epa-index")]
                [JsonPropertyName("us-epa-index")]
                public double? UsEpaIndex { get; set; }

                /// <summary>
                /// UK Defra Index
                /// <list type="bullet">
                /// <item><description>1 <b>Band</b> Low <b>µgm<sup>-3</sup></b> 0-11 </description></item>
                /// <item><description>2 <b>Band</b> Low <b>µgm<sup>-3</sup></b> 12-23 </description></item>
                /// <item><description>3 <b>Band</b> Low <b>µgm<sup>-3</sup></b> 24-35 </description></item>
                /// <item><description>4 <b>Band</b> Moderate <b>µgm<sup>-3</sup></b> 36-41 </description></item>
                /// <item><description>5 <b>Band</b> Moderate <b>µgm<sup>-3</sup></b> 42-47 </description></item>
                /// <item><description>6 <b>Band</b> Moderate <b>µgm<sup>-3</sup></b> 48-53 </description></item>
                /// <item><description>7 <b>Band</b> High <b>µgm<sup>-3</sup></b> 54-58 </description></item>
                /// <item><description>8 <b>Band</b> High <b>µgm<sup>-3</sup></b> 59-64 </description></item>
                /// <item><description>9 <b>Band</b> High <b>µgm<sup>-3</sup></b> 65-70 </description></item>
                /// <item><description>10 <b>Band</b> Very High <b>µgm<sup>-3</sup></b> 71 or more </description></item>
                /// </list>
                /// </summary>
                [JsonProperty("gb-defra-index")]
                [JsonPropertyName("gb-defra-index")]
                public double? GbDefraIndex { get; set; }
            }

            /// <summary>
            /// Local time when the real time data was updated in unix time.
            /// </summary>
            [JsonProperty("last_updated_epoch")]
            [JsonPropertyName("last_updated_epoch")]
            public double? LastUpdatedEpoch { get; set; }

            /// <summary>
            /// Local time when the real time data was updated.
            /// </summary>
            [JsonProperty("last_updated")]
            [JsonPropertyName("last_updated")]
            public string? LastUpdated { get; set; }

            /// <summary>
            /// Temperature in celsius
            /// </summary>
            [JsonProperty("temp_c")]
            [JsonPropertyName("temp_c")]
            public double? TempC { get; set; }

            /// <summary>
            /// Temperature in fahrenheit
            /// </summary>
            [JsonProperty("temp_f")]
            [JsonPropertyName("temp_f")]
            public double? TempF { get; set; }

            /// <summary>
            /// 1 = Yes 0 = No
            /// Whether to show day condition icon or night icon
            /// </summary>
            [JsonProperty("is_day")]
            [JsonPropertyName("is_day")]
            public double? IsDay { get; set; }

            [JsonProperty("condition")]
            [JsonPropertyName("condition")]
            public WeatherResponseCurrentCondition? Condition { get; set; }

            /// <summary>
            /// Wind speed in miles per hour
            /// </summary>
            [JsonProperty("wind_mph")]
            [JsonPropertyName("wind_mph")]
            public double? WindMph { get; set; }

            /// <summary>
            /// Wind speed in kilometer per hour
            /// </summary>
            [JsonProperty("wind_kph")]
            [JsonPropertyName("wind_kph")]
            public double? WindKph { get; set; }

            /// <summary>
            /// Wind direction in degrees
            /// </summary>
            [JsonProperty("wind_degree")]
            [JsonPropertyName("wind_degree")]
            public double? WindDegree { get; set; }

            /// <summary>
            /// Wind direction as 16 podouble compass. e.g.: NSW
            /// </summary>
            [JsonProperty("wind_dir")]
            [JsonPropertyName("wind_dir")]
            public string? WindDir { get; set; }

            /// <summary>
            /// Pressure in millibars
            /// </summary>
            [JsonProperty("pressure_mb")]
            [JsonPropertyName("pressure_mb")]
            public double? PressureMb { get; set; }

            /// <summary>
            /// Pressure in inches
            /// </summary>
            [JsonProperty("pressure_in")]
            [JsonPropertyName("pressure_in")]
            public double? PressureIn { get; set; }

            /// <summary>
            /// Precipitation amount in millimeters
            /// </summary>
            [JsonProperty("precip_mm")]
            [JsonPropertyName("precip_mm")]
            public double? PrecipMm { get; set; }

            /// <summary>
            /// Precipitation amount in inches
            /// </summary>
            [JsonProperty("precip_in")]
            [JsonPropertyName("precip_in")]
            public double? PrecipIn { get; set; }

            /// <summary>
            /// Humidity as percentage
            /// </summary>
            [JsonProperty("humidity")]
            [JsonPropertyName("humidity")]
            public double? Humidity { get; set; }

            /// <summary>
            /// Cloud cover as percentage
            /// </summary>
            [JsonProperty("cloud")]
            [JsonPropertyName("cloud")]
            public double? Cloud { get; set; }

            /// <summary>
            /// Feels like temperature in celsius
            /// </summary>
            [JsonProperty("feelslike_c")]
            [JsonPropertyName("feelslike_c")]
            public double? FeelsLikeC { get; set; }

            /// <summary>
            /// Feels like temperature in fahrenheit
            /// </summary>
            [JsonProperty("feelslike_f")]
            [JsonPropertyName("feelslike_f")]
            public double? FeelsLikeF { get; set; }

            [JsonProperty("windchill_c")]
            [JsonPropertyName("windchill_c")]
            public double? WindchillC { get; set; }

            [JsonProperty("windchill_f")]
            [JsonPropertyName("windchill_f")]
            public double? WindchillF { get; set; }

            [JsonProperty("heatindex_c")]
            [JsonPropertyName("heatindex_c")]
            public double? HeatindexC { get; set; }

            [JsonProperty("heatindex_f")]
            [JsonPropertyName("heatindex_f")]
            public double? HeatindexF { get; set; }

            [JsonProperty("dewpodouble_c")]
            [JsonPropertyName("dewpodouble_c")]
            public double? DewpodoubleC { get; set; }

            [JsonProperty("dewpodouble_f")]
            [JsonPropertyName("dewpodouble_f")]
            public double? DewpodoubleF { get; set; }

            /// <summary>
            /// Visibility in kilometer
            /// </summary>
            [JsonProperty("vis_km")]
            [JsonPropertyName("vis_km")]
            public double? VisKm { get; set; }

            /// <summary>
            /// Visibility in miles
            /// </summary>
            [JsonProperty("vis_miles")]
            [JsonPropertyName("vis_miles")]
            public double? VisMiles { get; set; }

            /// <summary>
            /// UV Index
            /// </summary>
            [JsonProperty("uv")]
            [JsonPropertyName("uv")]
            public double? Uv { get; set; }

            /// <summary>
            /// Wind gust in miles per hour
            /// </summary>
            [JsonProperty("gust_mph")]
            [JsonPropertyName("gust_mph")]
            public double? GustMph { get; set; }

            /// <summary>
            /// Wind gust in kilometer per hour
            /// </summary>
            [JsonProperty("gust_kph")]
            [JsonPropertyName("gust_kph")]
            public double? GustKph { get; set; }

            [JsonProperty("air_quality")]
            [JsonPropertyName("air_quality")]
            public WeatherResponseCurrentAirQuality? AirQuality { get; set; }
        }

        [JsonProperty("location")]
        [JsonPropertyName("location")]
        public WeatherResponseLocation? Location { get; set; }

        [JsonProperty("current")]
        [JsonPropertyName("current")]
        public WeatherResponseCurrent? Current { get; set; }
    }
}