using System.Globalization;
using System.Text.Json;

namespace RaceDayNova.Api.Services;

/// <summary>A single weather reading returned by a provider.</summary>
public record WeatherReading(decimal? TemperatureC, decimal? WindKph, decimal? RainChance, string? Summary);

public interface IWeatherProvider
{
    string Name { get; }

    /// <summary>Returns the current conditions for a city, or null when the provider cannot supply them.</summary>
    Task<WeatherReading?> GetCurrentAsync(string city, string province, CancellationToken ct);
}

/// <summary>
/// Live weather from Open-Meteo (free, no API key). Events store a city rather than coordinates, so a small
/// lookup of South African cities is used. Unknown cities return null (the endpoint then falls back to the
/// cached snapshot, or 503 when there is none).
/// </summary>
public class OpenMeteoWeatherProvider : IWeatherProvider
{
    private static readonly Dictionary<string, (double Lat, double Lon)> Cities =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Mbombela"] = (-25.4753, 30.9694),
            ["Nelspruit"] = (-25.4753, 30.9694),
            ["Cape Town"] = (-33.9249, 18.4241),
            ["Soweto"] = (-26.2485, 27.8540),
            ["Johannesburg"] = (-26.2041, 28.0473),
            ["Sandton"] = (-26.1076, 28.0567),
            ["Pretoria"] = (-25.7479, 28.2293),
            ["Tshwane"] = (-25.7479, 28.2293),
            ["Centurion"] = (-25.8603, 28.1894),
            ["Durban"] = (-29.8587, 31.0218),
            ["Pietermaritzburg"] = (-29.6006, 30.3794),
            ["Gqeberha"] = (-33.9608, 25.6022),
            ["Port Elizabeth"] = (-33.9608, 25.6022),
            ["East London"] = (-33.0153, 27.9116),
            ["Bloemfontein"] = (-29.0852, 26.1596),
            ["Polokwane"] = (-23.9045, 29.4689),
            ["Kimberley"] = (-28.7282, 24.7499),
            ["Stellenbosch"] = (-33.9321, 18.8602),
            ["George"] = (-33.9630, 22.4617),
            ["Rustenburg"] = (-25.6676, 27.2421)
        };

    private readonly HttpClient _http;

    public OpenMeteoWeatherProvider(HttpClient http)
    {
        _http = http;
    }

    public string Name => "Open-Meteo";

    public async Task<WeatherReading?> GetCurrentAsync(string city, string province, CancellationToken ct)
    {
        if (!Cities.TryGetValue(city.Trim(), out var c)) return null;

        var url = "https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={c.Lat.ToString(CultureInfo.InvariantCulture)}&longitude={c.Lon.ToString(CultureInfo.InvariantCulture)}" +
                  "&current=temperature_2m,wind_speed_10m,weather_code" +
                  "&daily=precipitation_probability_max&forecast_days=1&timezone=auto";

        using var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var current = doc.RootElement.GetProperty("current");
        decimal? temp = current.TryGetProperty("temperature_2m", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetDecimal() : null;
        decimal? wind = current.TryGetProperty("wind_speed_10m", out var w) && w.ValueKind == JsonValueKind.Number ? w.GetDecimal() : null;
        int? code = current.TryGetProperty("weather_code", out var wc) && wc.ValueKind == JsonValueKind.Number ? wc.GetInt32() : null;

        decimal? rain = null;
        if (doc.RootElement.TryGetProperty("daily", out var daily) &&
            daily.TryGetProperty("precipitation_probability_max", out var pm) &&
            pm.ValueKind == JsonValueKind.Array && pm.GetArrayLength() > 0 &&
            pm[0].ValueKind == JsonValueKind.Number)
        {
            rain = pm[0].GetDecimal();
        }

        return new WeatherReading(temp, wind, rain, Describe(code));
    }

    private static string? Describe(int? code) => code switch
    {
        0 => "Clear sky",
        1 or 2 => "Mostly clear to partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        >= 51 and <= 57 => "Drizzle",
        >= 61 and <= 67 => "Rain",
        >= 71 and <= 77 => "Snow",
        >= 80 and <= 82 => "Rain showers",
        >= 95 and <= 99 => "Thunderstorm",
        _ => null
    };
}
