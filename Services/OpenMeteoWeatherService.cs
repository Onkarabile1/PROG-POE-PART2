using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaceDay.API.Services;

public class OpenMeteoWeatherService : IWeatherService
{
    private const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";

    private readonly HttpClient _http;

    public OpenMeteoWeatherService(HttpClient http)
    {
        _http = http;
    }

    public async Task<WeatherReading> GetCurrentWeatherAsync(
        string city, string? province, CancellationToken cancellationToken = default)
    {
        try
        {
            var place = await FindPlaceAsync(city, province, cancellationToken);

            var latitude = place.Latitude.ToString(CultureInfo.InvariantCulture);
            var longitude = place.Longitude.ToString(CultureInfo.InvariantCulture);

            var url = $"{ForecastUrl}?latitude={latitude}&longitude={longitude}" +
                      "&current=temperature_2m,wind_speed_10m,weather_code&wind_speed_unit=kmh&timezone=auto";

            var forecast = await _http.GetFromJsonAsync<ForecastResponse>(url, cancellationToken);
            var current = forecast?.Current;

            if (current?.Temperature is null || current.WindSpeed is null)
                throw new WeatherUnavailableException("The weather service did not return current conditions.");

            return new WeatherReading(
                Math.Round(current.Temperature.Value, 1),
                Math.Round(current.WindSpeed.Value, 1),
                Describe(current.WeatherCode));
        }
        catch (HttpRequestException ex)
        {
            throw new WeatherUnavailableException("The weather service could not be reached.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WeatherUnavailableException("The weather service took too long to respond.", ex);
        }
        catch (JsonException ex)
        {
            throw new WeatherUnavailableException("The weather service returned data in an unexpected format.", ex);
        }
    }

    // Looks the city up in South Africa. If several places share the name, the one in the event's province wins.
    private async Task<GeoPlace> FindPlaceAsync(string city, string? province, CancellationToken cancellationToken)
    {
        var url = $"{GeocodingUrl}?name={Uri.EscapeDataString(city)}&count=10&language=en&format=json&countryCode=ZA";

        var response = await _http.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);
        var places = response?.Results ?? new List<GeoPlace>();

        var place = places.FirstOrDefault(p =>
                        !string.IsNullOrWhiteSpace(province) &&
                        string.Equals(p.Province, province, StringComparison.OrdinalIgnoreCase))
                    ?? places.FirstOrDefault();

        if (place is null)
            throw new WeatherLocationNotFoundException($"No location named '{city}' was found for the weather lookup.");

        return place;
    }

    // WMO weather codes used by Open-Meteo.
    private static string Describe(int? code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 => "Freezing drizzle",
        61 => "Light rain",
        63 => "Rain",
        65 => "Heavy rain",
        66 or 67 => "Freezing rain",
        71 or 73 or 75 => "Snow",
        77 => "Snow grains",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        97 => "Heavy thunderstorm",
        _ => "Unknown"
    };

    private sealed class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeoPlace>? Results { get; set; }
    }

    private sealed class GeoPlace
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("admin1")]
        public string? Province { get; set; }
    }

    private sealed class ForecastResponse
    {
        [JsonPropertyName("current")]
        public CurrentConditions? Current { get; set; }
    }

    private sealed class CurrentConditions
    {
        [JsonPropertyName("temperature_2m")]
        public decimal? Temperature { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public decimal? WindSpeed { get; set; }

        [JsonPropertyName("weather_code")]
        public int? WeatherCode { get; set; }
    }
}