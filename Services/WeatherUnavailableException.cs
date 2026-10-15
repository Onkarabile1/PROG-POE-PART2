namespace RaceDay.API.Services;

public record WeatherReading(decimal TemperatureC, decimal WindSpeedKmh, string Condition);

// The external weather API could not be reached or returned something unusable (becomes a 502).
public class WeatherUnavailableException : Exception
{
    public WeatherUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

// The event's city could not be found by the weather service.
public class WeatherLocationNotFoundException : Exception
{
    public WeatherLocationNotFoundException(string message) : base(message) { }
}

public interface IWeatherService
{
    Task<WeatherReading> GetCurrentWeatherAsync(
        string city, string? province, CancellationToken cancellationToken = default);
}